using System.Collections.Concurrent;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using SslCertHub.Abstractions;

namespace SslCertHub.Services;

public class LetsEncryptCertProvider : ICertProvider, IDisposable
{
    private readonly LetsEncryptCertProviderOptions _options;

    private readonly ConcurrentDictionary<string, (IOrderContext orderContext, IChallengeContext challengeContext)>
        _orderContexts = new();

    public LetsEncryptCertProvider(LetsEncryptCertProviderOptions options)
    {
        _options = options;
    }

    public async ValueTask<string> DnsTxtAsync(string domain)
    {
        var acmeContext = await CreateAcmeContextAsync();

        var order = await acmeContext.NewOrder(new List<string> { domain });
        var authorizations = await order.Authorizations();
        var challenge = await authorizations.First().Dns();

        var dnsTxt = acmeContext.AccountKey.DnsTxt(challenge.Token);

        _orderContexts[domain] = (order, challenge);

        return dnsTxt;
    }

    private async Task<AcmeContext> CreateAcmeContextAsync()
    {
        //todo Load account key from storage
        string? pemKey = null;

        if (pemKey == null)
        {
            var acmeContext = new AcmeContext(WellKnownServers.LetsEncryptV2);
            await acmeContext.NewAccount(_options.Email, true);
            pemKey = acmeContext.AccountKey.ToPem();
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
                throw new Exception("DNS validation failed. Error:" + challenge.Error);
            }

            if (challenge.Status != ChallengeStatus.Valid)
            {
                await Task.Delay(30000);
                continue;
            }

            break;
        }

        var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
        var cert = await orderContext.orderContext.Generate(new CsrInfo(), privateKey);

        var certPem = cert.ToPem();

        return new Certificate(domain: domain, pemPublicKey: certPem, pemPrivateKey: privateKey.ToPem());
    }

    public void Dispose()
    {
        _orderContexts.Clear();
    }
}