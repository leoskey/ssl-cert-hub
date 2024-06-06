using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SslCertHub.Abstractions;
using SslCertHub.Plugins;
using SslCertHub.Services;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace SslCertHub;

public class SslCertHubModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<AlibabaCloudDnsProviderOptions>(configuration.GetSection("DnsProvider:AlibabaCloud"));
        Configure<LetsEncryptCertProviderOptions>(configuration.GetSection("CertProvider:LetsEncrypt"));
        Configure<AlibabaCloudCasPluginOptions>(configuration.GetSection("Plugin:AlibabaCloudCas"));
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = context.ServiceProvider.GetRequiredService<ILogger<SslCertHubModule>>();

        var domains = configuration.GetSection("Domains").Get<List<string>>();
        if (domains is not { Count: > 0 })
        {
            logger.LogInformation("No domains to generate certificates");
            return;
        }

        var dnsProvider = context.ServiceProvider.GetRequiredService<IDnsProvider>();
        var certProvider = context.ServiceProvider.GetRequiredService<ICertProvider>();
        var certManager = new SslCertManager(certProvider, dnsProvider);

        var certManagerPlugins = context.ServiceProvider.GetServices<ICertManagerPlugin>();
        certManager.AddPlugins(certManagerPlugins);

        foreach (var domain in domains)
        {
            await certManager.GenerateCertAsync(domain);
        }
    }
}