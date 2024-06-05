namespace SslCertHub.Abstractions;

public interface ICertProvider
{
    ValueTask<string> DnsTxtAsync(string domain);
    Task<Certificate> ChallengeAsync(string domain);
}