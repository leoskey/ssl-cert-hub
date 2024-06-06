using System.Reflection;
using System.Text;
using SslCertHub.Abstractions;
using Volo.Abp.DependencyInjection;

namespace SslCertHub.Services;

public class FileStorageManager : IFileStorageManager, ISingletonDependency
{
    private readonly string _configDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        $".{Assembly.GetExecutingAssembly().GetName().Name}"
    );

    private const string KeyFileName = "letsencrypt.key";

    private const string PublicKeyFileExtension = ".pem";

    public FileStorageManager()
    {
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

    public async Task<Certificate?> GetCertificateAsync(string domain)
    {
        var publicKeyPath = Path.Combine(_configDirectoryPath, $"{domain}.pem");
        if (!File.Exists(publicKeyPath))
        {
            return null;
        }

        var privateKeyPath = Path.Combine(_configDirectoryPath, $"{domain}.key");
        if (!File.Exists(privateKeyPath))
        {
            return null;
        }

        var publicKey = await File.ReadAllTextAsync(publicKeyPath, Encoding.UTF8);
        var privateKey = await File.ReadAllTextAsync(privateKeyPath, Encoding.UTF8);

        return new Certificate(
            domain: domain,
            pemPublicKey: publicKey,
            pemPrivateKey: privateKey
        );
    }

    public async Task SaveCertificateAsync(Certificate certificate)
    {
        var publicKeyPath = Path.Combine(_configDirectoryPath, $"{certificate.Domain}.pem");
        var privateKeyPath = Path.Combine(_configDirectoryPath, $"{certificate.Domain}.key");
        await File.WriteAllTextAsync(publicKeyPath, certificate.PemPublicKey, Encoding.UTF8);
        await File.WriteAllTextAsync(privateKeyPath, certificate.PemPrivateKey, Encoding.UTF8);
    }
}