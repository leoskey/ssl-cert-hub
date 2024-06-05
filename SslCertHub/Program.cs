using SslCertHub;
using SslCertHub.Plugins;
using SslCertHub.Services;

var email = "";
var domain = "";
var accessKeyId = "";
var accessKeySecret = "";

var dnsProvider = new AlibabaCloudDnsProvider(new AlibabaCloudDnsProviderOptions
{
    AccessKeyId = accessKeyId,
    AccessKeySecret = accessKeySecret,
});
var certProvider = new LetsEncryptCertProvider(new LetsEncryptCertProviderOptions
{
    Email = email
});

var certManager = new SslCertManager(certProvider, dnsProvider);

certManager.AddPlugin(new AlibabaCloudCasPlugin(new AlibabaCloudCasPluginOptions
{
    AccessKeyId = accessKeyId,
    AccessKeySecret = accessKeySecret,
}));

var certificate = await certManager.GenerateCertAsync(domain);