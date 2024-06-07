namespace SslCertHub.Abstractions;

public interface ICertProvider
{
    ValueTask<string> DnsTxtAsync(string domain);
    Task<CertificateInfo> ChallengeAsync(string domain);
}