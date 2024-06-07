namespace SslCertHub.Services;

public class DomainOptions
{
    public required string DomainName { get; set; }

    public List<string>? Plugins { get; set; }
}