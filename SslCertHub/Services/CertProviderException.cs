namespace SslCertHub.Services;

public class CertProviderException : Exception
{
    public CertProviderException(string? message) : base(message)
    {
    }

    public CertProviderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}