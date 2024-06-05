namespace SslCertHub.Abstractions;

public interface ICertManagerPlugin
{
    Task OnCertGenerated(Certificate certificate);
}