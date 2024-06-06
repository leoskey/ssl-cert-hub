using SslCertHub.Abstractions;

namespace SslCertHub;

public class SslCertManager
{
    private readonly ICertProvider _certProvider;
    private readonly IDnsProvider _dnsProvider;
    private readonly List<ICertManagerPlugin> _plugins = new();

    public SslCertManager(ICertProvider certProvider, IDnsProvider dnsProvider)
    {
        _certProvider = certProvider;
        _dnsProvider = dnsProvider;
    }

    public async Task<Certificate> GenerateCertAsync(string domain)
    {
        string? dnsRecordId = null;
        try
        {
            var dnsText = await _certProvider.DnsTxtAsync(domain);
            dnsRecordId = await _dnsProvider.AddDnsTxtRecordAsync(domain, dnsText);
            var certificate = await _certProvider.ChallengeAsync(domain);
            foreach (var certManagerPlugin in _plugins)
            {
                await certManagerPlugin.OnCertGenerated(certificate);
            }

            return certificate;
        }
        finally
        {
            if (dnsRecordId != null)
            {
                await _dnsProvider.DeleteDnsRecordAsync(dnsRecordId);
            }
        }
    }

    public void AddPlugins(IEnumerable<ICertManagerPlugin> plugins)
    {
        _plugins.AddRange(plugins);
    }
}