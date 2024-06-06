using Microsoft.Extensions.DependencyInjection;
using SslCertHub.Services;
using Volo.Abp.Modularity;

namespace SslCertHub;

public class SslCertHubModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<AlibabaCloudProviderOptions>(configuration.GetSection("CloudProvider:AlibabaCloud"));
        Configure<LetsEncryptCertProviderOptions>(configuration.GetSection("CertProvider:LetsEncrypt"));
        Configure<List<DomainOptions>>(configuration.GetSection("Domains"));
    }
}