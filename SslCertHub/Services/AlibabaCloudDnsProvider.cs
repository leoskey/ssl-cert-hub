using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SslCertHub.Abstractions;
using Volo.Abp.DependencyInjection;

namespace SslCertHub.Services;

public class AlibabaCloudDnsProvider : IDnsProvider, ITransientDependency
{
    private readonly ILogger<AlibabaCloudDnsProvider> _logger;
    private readonly Client _client;

    public AlibabaCloudDnsProvider(
        ILogger<AlibabaCloudDnsProvider> logger,
        IOptionsMonitor<AlibabaCloudDnsProviderOptions> options
    )
    {
        _logger = logger;
        var config = new Config
        {
            AccessKeyId = options.CurrentValue.AccessKeyId,
            AccessKeySecret = options.CurrentValue.AccessKeySecret,
        };

        _client = new Client(config);
    }

    public async ValueTask<string> AddDnsTxtRecordAsync(string domain, string dnsTxt)
    {
        var domainSegment = domain.Split('.');
        var domainName = string.Join('.', domainSegment[^2..]);
        var subDomain = domainSegment.Length == 2 ? [] : domainSegment[..^2];

        var rr = string.Join('.', new List<string> { "_acme-challenge" }.Concat(subDomain));

        _logger.LogInformation("Adding DNS TXT record for {Domain} with RR {RR} and value {Value}", domain, rr, dnsTxt);

        var addDomainRecordRequest = new AddDomainRecordRequest
        {
            DomainName = domainName,
            RR = rr,
            Type = "TXT",
            Value = dnsTxt
        };
        var addDomainRecordResponse1 = await _client.AddDomainRecordAsync(addDomainRecordRequest);

        _logger.LogInformation("DNS TXT record added with RecordId {RecordId}", addDomainRecordResponse1.Body.RecordId);

        return addDomainRecordResponse1.Body.RecordId;
    }

    public async ValueTask DeleteDnsRecordAsync(string recordId)
    {
        var deleteDomainRecordRequest = new DeleteDomainRecordRequest
        {
            RecordId = recordId
        };
        _logger.LogInformation("Deleting DNS TXT record with RecordId {RecordId}", recordId);
        await _client.DeleteDomainRecordAsync(deleteDomainRecordRequest);
    }
}