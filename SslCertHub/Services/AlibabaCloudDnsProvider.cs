using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using SslCertHub.Abstractions;

namespace SslCertHub.Services;

public class AlibabaCloudDnsProvider : IDnsProvider
{
    private readonly Client _client;

    public AlibabaCloudDnsProvider(AlibabaCloudDnsProviderOptions options)
    {
        var config = new Config
        {
            AccessKeyId = options.AccessKeyId,
            AccessKeySecret = options.AccessKeySecret,
        };

        _client = new Client(config);
    }

    public async ValueTask<string> AddDnsTxtRecordAsync(string domain, string dnsTxt)
    {
        var domainSegment = domain.Split('.');
        var domainName = string.Join('.', domainSegment[^2..]);
        var subDomain = domainSegment.Length == 2 ? ["@"] : domainSegment[..^2];

        var rr = string.Join('.', new List<string> { "_acme-challenge" }.Concat(subDomain));

        var addDomainRecordRequest = new AddDomainRecordRequest
        {
            DomainName = domainName,
            RR = rr,
            Type = "TXT",
            Value = dnsTxt
        };
        var addDomainRecordResponse1 = await _client.AddDomainRecordAsync(addDomainRecordRequest);
        return addDomainRecordResponse1.Body.RecordId;
    }

    public async ValueTask DeleteDnsRecordAsync(string recordId)
    {
        var deleteDomainRecordRequest = new DeleteDomainRecordRequest
        {
            RecordId = recordId
        };
        await _client.DeleteDomainRecordAsync(deleteDomainRecordRequest);
    }
}