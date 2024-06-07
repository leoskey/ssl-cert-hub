namespace SslCertHub.Abstractions;

public interface IFileStorageManager
{
    Task<string?> GetAccountKeyAsync();
    Task SetAccountKeyAsync(string pemKey);
    Task<CertificateInfo?> GetCertificateAsync(string domain);
    Task SaveCertificateAsync(CertificateInfo certificateInfo);
}