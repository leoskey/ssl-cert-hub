using System.Security.Cryptography.X509Certificates;

namespace SslCertHub.Abstractions;

public class Certificate
{
    public Certificate(string domain, string pemPublicKey, string pemPrivateKey)
    {
        Domain = domain;
        PemPublicKey = pemPublicKey;
        PemPrivateKey = pemPrivateKey;

        var base64 = pemPublicKey
            .Replace("-----BEGIN CERTIFICATE-----", "")
            .Split("-----END CERTIFICATE-----")
            .Last(t => !t.IsNullOrWhiteSpace())
            .Trim();

        var bytes = Convert.FromBase64String(base64);
        X509 = new X509Certificate2(bytes);
    }

    private X509Certificate2 X509 { get; }

    public string Domain { get; private set; }

    public string PemPublicKey { get; private set; }

    public string PemPrivateKey { get; private set; }
    public bool Expired => DateTime.Now > X509.NotAfter;
}