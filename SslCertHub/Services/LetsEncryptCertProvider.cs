using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SslCertHub.Abstractions;
using Volo.Abp.DependencyInjection;
using Directory = System.IO.Directory;

namespace SslCertHub.Services;

public class LetsEncryptCertProvider : ICertProvider, IDisposable, ITransientDependency
{
    private readonly ILogger<LetsEncryptCertProvider> _logger;
    private readonly IOptionsMonitor<LetsEncryptCertProviderOptions> _options;

    private readonly ConcurrentDictionary<string, (IOrderContext orderContext, IChallengeContext challengeContext)>
        _orderContexts = new();

    public LetsEncryptCertProvider(
        ILogger<LetsEncryptCertProvider> logger,
        IOptionsMonitor<LetsEncryptCertProviderOptions> options
    )
    {
        _logger = logger;
        _options = options;
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
        var pemKey = GetAccountKey();

        if (pemKey == null)
        {
            var acmeContext = new AcmeContext(WellKnownServers.LetsEncryptV2);
            await acmeContext.NewAccount(_options.CurrentValue.Email, true);
            pemKey = acmeContext.AccountKey.ToPem();
            SetAccountKey(pemKey);
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

    private readonly string _configDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        $".{Assembly.GetExecutingAssembly().GetName().Name}"
    );

    private const string KeyFileName = "letsencrypt.key";

    private string? GetAccountKey()
    {
        var filePath = Path.Combine(_configDirectoryPath, KeyFileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath, Encoding.UTF8) : null;
    }

    private void SetAccountKey(string key)
    {
        if (!Directory.Exists(_configDirectoryPath))
        {
            Directory.CreateDirectory(_configDirectoryPath);
        }

        var filePath = Path.Combine(_configDirectoryPath, KeyFileName);
        File.WriteAllText(filePath, key, Encoding.UTF8);
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

        return new Certificate(domain: domain, pemPublicKey: certPem, pemPrivateKey: privateKey.ToPem());
    }

    public void Dispose()
    {
        _orderContexts.Clear();
    }
}