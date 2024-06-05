namespace SslCertHub.Abstractions;

public class Certificate
{
    public Certificate(string domain, string pemPublicKey, string pemPrivateKey)
    {
        Domain = domain;
        PemPublicKey = pemPublicKey;
        PemPrivateKey = pemPrivateKey;
    }

    public string Domain { get; private set; }

    public string PemPublicKey { get; private set; }

    public string PemPrivateKey { get; private set; }
}