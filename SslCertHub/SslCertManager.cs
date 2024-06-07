using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SslCertHub.Abstractions;
using SslCertHub.Services;
using Volo.Abp.DependencyInjection;

namespace SslCertHub;

public class SslCertManager : ISingletonDependency
{
    private readonly ILogger<SslCertManager> _logger;
    private readonly ICertProvider _certProvider;
    private readonly IDnsProvider _dnsProvider;
    private readonly IOptionsMonitor<List<DomainOptions>> _domainOptions;
    private readonly IFileStorageManager _fileStorageManager;
    private readonly IEnumerable<ISslCertHubPlugin>? _plugins;

    public SslCertManager(
        ILogger<SslCertManager> logger,
        ICertProvider certProvider,
        IDnsProvider dnsProvider,
        IOptionsMonitor<List<DomainOptions>> domainOptions,
        IFileStorageManager fileStorageManager,
        IEnumerable<ISslCertHubPlugin>? plugins
    )
    {
        _logger = logger;
        _certProvider = certProvider;
        _dnsProvider = dnsProvider;
        _domainOptions = domainOptions;
        _fileStorageManager = fileStorageManager;
        _plugins = plugins;
    }

    public async Task RunAsync()
    {
        if (_domainOptions.CurrentValue is not { Count: > 0 })
        {
            _logger.LogInformation("No domains to generate certificates");
            return;
        }

        foreach (var domain in _domainOptions.CurrentValue)
        {
            await RunTaskAsync(domain);
        }
    }

    private async Task RunTaskAsync(DomainOptions domainOptions)
    {
        var certificateInfo = await _fileStorageManager.GetCertificateAsync(domainOptions.DomainName);
        if (certificateInfo != null && certificateInfo.Certificate.NotAfter > DateTime.Now.AddDays(7))
        {
            _logger.LogInformation("Certificate for domain {Domain} is still valid", domainOptions.DomainName);
            return;
        }

        string? dnsRecordId = null;
        try
        {
            var dnsText = await _certProvider.DnsTxtAsync(domainOptions.DomainName);
            dnsRecordId = await _dnsProvider.AddDnsTxtRecordAsync(domainOptions.DomainName, dnsText);
            certificateInfo = await _certProvider.ChallengeAsync(domainOptions.DomainName);
            await _fileStorageManager.SaveCertificateAsync(certificateInfo);

            await RunPluginsAsync(domainOptions, certificateInfo);
        }
        finally
        {
            if (dnsRecordId != null)
            {
                await _dnsProvider.DeleteDnsRecordAsync(dnsRecordId);
            }
        }
    }

    private async Task RunPluginsAsync(DomainOptions domain, CertificateInfo certificateInfo)
    {
        if (_plugins == null || !_plugins.Any())
        {
            return;
        }

        var plugins = _plugins
            .Where(plugin => domain.Plugins.Contains(plugin.GetType().Name))
            .ToList();

        foreach (var certManagerPlugin in plugins)
        {
            try
            {
                await certManagerPlugin.OnCertGenerated(certificateInfo);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error running plugin {Plugin}", certManagerPlugin.GetType().Name);
            }
        }
    }
}