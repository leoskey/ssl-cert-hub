using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Cas20200407;
using AlibabaCloud.SDK.Cas20200407.Models;
using SslCertHub.Abstractions;

namespace SslCertHub.Plugins;

public class AlibabaCloudCasPlugin : ICertManagerPlugin
{
    private readonly Client _client;

    public AlibabaCloudCasPlugin(AlibabaCloudCasPluginOptions options)
    {
        var config = new Config
        {
            AccessKeyId = options.AccessKeyId,
            AccessKeySecret = options.AccessKeySecret,
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
    }
}