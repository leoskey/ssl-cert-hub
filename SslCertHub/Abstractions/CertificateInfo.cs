using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

namespace SslCertHub.Abstractions;

public class CertificateInfo
{
    // public CertificateInfo(string domain, string publicKey, string privateKey)
    // {
    //     Domain = domain;
    //     PublicKey = publicKey;
    //     PrivateKey = privateKey;
    //
    //     var base64 = publicKey
    //         .Replace("-----BEGIN CERTIFICATE-----", "")
    //         .Split("-----END CERTIFICATE-----")
    //         .Last(t => !t.IsNullOrWhiteSpace())
    //         .Trim();
    //
    //     var bytes = Convert.FromBase64String(base64);
    //     Certificate = new X509Certificate2(bytes);
    // }
    public const string? DefaultCertificatePassword = null;

    public CertificateInfo(string domain, byte[] pfx, string? password = DefaultCertificatePassword)
    {
        Domain = domain;
        Certificate = new X509Certificate2(pfx, password);

        var (publicKey, privateKey) = GetPem(pfx, password);
        PublicKey = publicKey;
        PrivateKey = privateKey;
    }

    public X509Certificate2 Certificate { get; private set; }

    public string Domain { get; private set; }

    public string PublicKey { get; private set; }

    public string PrivateKey { get; private set; }

    private (string publicKey, string privateKey) GetPem(byte[] pfx, string? password)
    {
        var pkcs12Store = new Pkcs12Store(new MemoryStream(pfx),
            password.IsNullOrWhiteSpace() ? Array.Empty<char>() : password.ToCharArray());

        var publicKey = string.Empty;
        var privateKey = string.Empty;

        foreach (string n in pkcs12Store.Aliases)
        {
            if (pkcs12Store.IsKeyEntry(n))
            {
                var key = pkcs12Store.GetKey(n).Key;
                TextWriter textWriter = new StringWriter();
                var pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(key);
                privateKey = textWriter.ToString();
            }

            var chain = pkcs12Store.GetCertificateChain(n);
            if (chain.Length > 0)
            {
                var entry = chain[0];
                TextWriter textWriter = new StringWriter();
                var pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(entry.Certificate);
                publicKey = textWriter.ToString();
            }
        }

        return (publicKey!, privateKey!);
    }
}