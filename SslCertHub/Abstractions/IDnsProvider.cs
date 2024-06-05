namespace SslCertHub.Abstractions;

public interface IDnsProvider
{
    ValueTask<string> AddDnsTxtRecordAsync(string domain, string dnsTxt);
    ValueTask DeleteDnsRecordAsync(string recordId);
}