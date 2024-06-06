using System.Collections.Concurrent;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SslCertHub.Abstractions;
using Volo.Abp.DependencyInjection;

namespace SslCertHub.Services;

public class LetsEncryptCertProvider : ICertProvider, IDisposable, ITransientDependency
{
    private readonly ILogger<LetsEncryptCertProvider> _logger;
    private readonly IOptionsMonitor<LetsEncryptCertProviderOptions> _options;
    private readonly IFileStorageManager _fileStorageManager;

    private readonly ConcurrentDictionary<string, (IOrderContext orderContext, IChallengeContext challengeContext)>
        _orderContexts = new();

    public LetsEncryptCertProvider(
        ILogger<LetsEncryptCertProvider> logger,
        IOptionsMonitor<LetsEncryptCertProviderOptions> options,
        IFileStorageManager fileStorageManager
    )
    {
        _logger = logger;
        _options = options;
        _fileStorageManager = fileStorageManager;
    }

    public async ValueTask<string> DnsTxtAsync(string domain)
    {
        _logger.LogInformation("Requesting DNS TXT record for domain {Domain}", domain);

        var acmeContext = await CreateAcmeContextAsync();

        var order = await acmeContext.NewOrder(new List<string> { domain });
        var authorizations = await order.Authorizations();
        var challenge = await authorizations.First().Dns();

        var dnsTxt = acmeContext.AccountKey.DnsTxt(challenge.Token);
        _logger.LogInformation("DNS TXT record for domain {Domain}: {DnsTxt}", domain, dnsTxt);

        _orderContexts[domain] = (order, challenge);

        return dnsTxt;
    }

    private async Task<AcmeContext> CreateAcmeContextAsync()
    {
        var pemKey = await _fileStorageManager.GetAccountKeyAsync();

        if (pemKey == null)
        {
            var acmeContext = new AcmeContext(WellKnownServers.LetsEncryptV2);
            await acmeContext.NewAccount(_options.CurrentValue.Email, true);
            pemKey = acmeContext.AccountKey.ToPem();
            await _fileStorageManager.SetAccountKeyAsync(pemKey);
            return acmeContext;
        }
        else
        {
            var accountKey = KeyFactory.FromPem(pemKey);
            var acmeContext = new AcmeContext(WellKnownServers.LetsEncryptV2, accountKey);
            await acmeContext.Account();
            return acmeContext;
        }
    }

    public async Task<Certificate> ChallengeAsync(string domain)
    {
        var orderContext = _orderContexts[domain];

        await orderContext.challengeContext.Validate();

        while (true)
        {
            var challenge = await orderContext.challengeContext.Resource();

            if (challenge.Status == ChallengeStatus.Invalid)
            {
                throw new CertProviderException("DNS validation failed. Error:" + challenge.Error);
            }

            if (challenge.Status != ChallengeStatus.Valid)
            {
                _logger.LogInformation("Waiting for DNS validation to complete for domain {Domain}", domain);
                await Task.Delay(30000);
                continue;
            }

            break;
        }

        var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
        var cert = await orderContext.orderContext.Generate(new CsrInfo(), privateKey);

        var certPem = cert.ToPem();

        _logger.LogInformation("Certificate generated for domain {Domain}", domain);

        return new Certificate(
            domain: domain,
            pemPublicKey: certPem,
            pemPrivateKey: privateKey.ToPem()
        );
    }

    public void Dispose()
    {
        _orderContexts.Clear();
    }
}