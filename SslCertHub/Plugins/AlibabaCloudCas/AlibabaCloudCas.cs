using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Cas20200407;
using AlibabaCloud.SDK.Cas20200407.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SslCertHub.Abstractions;
using SslCertHub.Services;
using Volo.Abp.DependencyInjection;

namespace SslCertHub.Plugins.AlibabaCloudCas;

[ExposeServices(typeof(ISslCertHubPlugin))]
public class AlibabaCloudCas : ISslCertHubPlugin, ITransientDependency
{
    private readonly ILogger<AlibabaCloudCas> _logger;
    private readonly Client _client;

    public AlibabaCloudCas(
        ILogger<AlibabaCloudCas> logger,
        IOptionsMonitor<AlibabaCloudProviderOptions> options
    )
    {
        _logger = logger;
        var config = new Config
        {
            AccessKeyId = options.CurrentValue.AccessKeyId,
            AccessKeySecret = options.CurrentValue.AccessKeySecret,
            Endpoint = "cas.aliyuncs.com"
        };
        _client = new Client(config);
    }

    public async Task OnCertGenerated(CertificateInfo certificateInfo)
    {
        var uploadUserCertificateRequest = new UploadUserCertificateRequest
        {
            Cert = certificateInfo.PublicKey,
            Key = certificateInfo.PrivateKey,
            Name = $"{certificateInfo.Domain}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
        };
        await _client.UploadUserCertificateAsync(uploadUserCertificateRequest);
        _logger.LogInformation("Certificate uploaded to Alibaba Cloud CAS");
    }
}