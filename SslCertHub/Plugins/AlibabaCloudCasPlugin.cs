using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Cas20200407;
using AlibabaCloud.SDK.Cas20200407.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SslCertHub.Abstractions;
using Volo.Abp.DependencyInjection;

namespace SslCertHub.Plugins;

[ExposeServices(typeof(ICertManagerPlugin))]
public class AlibabaCloudCasPlugin : ICertManagerPlugin, ITransientDependency
{
    private readonly ILogger<AlibabaCloudCasPlugin> _logger;
    private readonly Client _client;

    public AlibabaCloudCasPlugin(
        ILogger<AlibabaCloudCasPlugin> logger,
        IOptionsMonitor<AlibabaCloudCasPluginOptions> options)
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

    public async Task OnCertGenerated(Certificate certificate)
    {
        var uploadUserCertificateRequest = new UploadUserCertificateRequest
        {
            Cert = certificate.PemPublicKey,
            Key = certificate.PemPrivateKey,
            Name = $"{certificate.Domain}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
        };
        await _client.UploadUserCertificateAsync(uploadUserCertificateRequest);
        _logger.LogInformation("Certificate uploaded to Alibaba Cloud CAS");
    }
}