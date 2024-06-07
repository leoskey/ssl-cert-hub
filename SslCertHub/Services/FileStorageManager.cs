using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using SslCertHub.Abstractions;
using Volo.Abp.DependencyInjection;

namespace SslCertHub.Services;

public class FileStorageManager : IFileStorageManager, ISingletonDependency
{
    private readonly ILogger<FileStorageManager> _logger;

    private static string _configDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        $".{Assembly.GetExecutingAssembly().GetName().Name}"
    );

    private const string KeyFileName = "letsencrypt.key";

    private const string KeyFileExtension = ".pfx";

    public FileStorageManager(
        ILogger<FileStorageManager> logger
    )
    {
        _logger = logger;
        if (!Directory.Exists(_configDirectoryPath))
        {
            Directory.CreateDirectory(_configDirectoryPath);
        }
    }

    public async Task<string?> GetAccountKeyAsync()
    {
        var filePath = Path.Combine(_configDirectoryPath, KeyFileName);
        return File.Exists(filePath) ? await File.ReadAllTextAsync(filePath, Encoding.UTF8) : null;
    }

    public async Task SetAccountKeyAsync(string key)
    {
        var filePath = Path.Combine(_configDirectoryPath, KeyFileName);
        await File.WriteAllTextAsync(filePath, key, Encoding.UTF8);
    }

    public async Task<CertificateInfo?> GetCertificateAsync(string domain)
    {
        var filePath = Path.Combine(_configDirectoryPath, $"{domain}.{KeyFileExtension}");
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(filePath);
            return new CertificateInfo(domain, bytes);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error reading certificate file {FilePath}", filePath);
            return null;
        }
    }

    public async Task SaveCertificateAsync(CertificateInfo certificateInfo)
    {
        var publicKeyPath = Path.Combine(_configDirectoryPath, $"{certificateInfo.Domain}.pem");
        var privateKeyPath = Path.Combine(_configDirectoryPath, $"{certificateInfo.Domain}.key");
        await File.WriteAllTextAsync(publicKeyPath, certificateInfo.PublicKey, Encoding.UTF8);
        await File.WriteAllTextAsync(privateKeyPath, certificateInfo.PrivateKey, Encoding.UTF8);
    }
}