namespace SslCertHub.Abstractions;

public interface ISslCertHubPlugin
{
    Task OnCertGenerated(Certificate certificate);
}