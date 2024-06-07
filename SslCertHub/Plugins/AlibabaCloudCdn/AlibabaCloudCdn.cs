using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Cdn20180510;
using AlibabaCloud.SDK.Cdn20180510.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SslCertHub.Abstractions;
using SslCertHub.Services;
using Volo.Abp.DependencyInjection;

namespace SslCertHub.Plugins.AlibabaCloudCdn;

[ExposeServices(typeof(ISslCertHubPlugin))]
public class AlibabaCloudCdn : ISslCertHubPlugin, ITransientDependency
{
    private readonly ILogger<AlibabaCloudCdn> _logger;
    private readonly Client _client;

    public AlibabaCloudCdn(
        ILogger<AlibabaCloudCdn> logger,
        IOptionsMonitor<AlibabaCloudProviderOptions> optionsMonitor
    )
    {
        _logger = logger;
        var config = new Config
        {
            AccessKeyId = optionsMonitor.CurrentValue.AccessKeyId,
            AccessKeySecret = optionsMonitor.CurrentValue.AccessKeySecret,
            Endpoint = "cdn.aliyuncs.com"
        };
        _client = new Client(config);
    }

    public async Task OnCertGenerated(CertificateInfo certificateInfo)
    {
        _logger.LogInformation("Uploading certificate to Alibaba Cloud CDN");
        var request = new SetCdnDomainSSLCertificateRequest
        {
            DomainName = certificateInfo.Domain,
            CertType = "upload",
            SSLProtocol = "on",
            SSLPub = certificateInfo.PublicKey,
            SSLPri = certificateInfo.PrivateKey
        };
        await _client.SetCdnDomainSSLCertificateAsync(request);
        _logger.LogInformation("Certificate uploaded to Alibaba Cloud CDN");
    }
}