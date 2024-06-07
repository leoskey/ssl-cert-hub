namespace SslCertHub.Abstractions;

public interface ISslCertHubPlugin
{
    Task OnCertGenerated(CertificateInfo certificateInfo);
}