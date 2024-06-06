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
            await GenerateCertAsync(domain);
        }
    }

    private async Task<Certificate> GenerateCertAsync(DomainOptions domainOptions)
    {
        var certificate = await _fileStorageManager.GetCertificateAsync(domainOptions.DomainName);
        if (certificate is { Expired: false })
        {
            return certificate;
        }

        string? dnsRecordId = null;
        try
        {
            var dnsText = await _certProvider.DnsTxtAsync(domainOptions.DomainName);
            dnsRecordId = await _dnsProvider.AddDnsTxtRecordAsync(domainOptions.DomainName, dnsText);
            certificate = await _certProvider.ChallengeAsync(domainOptions.DomainName);
            await _fileStorageManager.SaveCertificateAsync(certificate);

            await RunPluginsAsync(domainOptions, certificate);

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

    private async Task RunPluginsAsync(DomainOptions domain, Certificate certificate)
    {
        var plugins = _plugins
            .Where(plugin => domain.Plugins.Contains(plugin.GetType().Name))
            .ToList();

        foreach (var certManagerPlugin in plugins)
        {
            try
            {
                await certManagerPlugin.OnCertGenerated(certificate);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error running plugin {Plugin}", certManagerPlugin.GetType().Name);
            }
        }
    }
}