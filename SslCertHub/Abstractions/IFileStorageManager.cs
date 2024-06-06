namespace SslCertHub.Abstractions;

public interface IFileStorageManager
{
    Task<string?> GetAccountKeyAsync();
    Task SetAccountKeyAsync(string pemKey);
    Task<Certificate> GetCertificateAsync(string domain);
    Task SaveCertificateAsync(Certificate certificate);
}