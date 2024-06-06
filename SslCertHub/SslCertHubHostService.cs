using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SslCertHub;

public class SslCertHubHostService : BackgroundService
{
    private readonly ILogger<SslCertHubHostService> _logger;
    private readonly SslCertManager _sslCertManager;

    public SslCertHubHostService(
        ILogger<SslCertHubHostService> logger,
        SslCertManager sslCertManager
        )
    {
        _logger = logger;
        _sslCertManager = sslCertManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            await _sslCertManager.RunAsync();

            _logger.LogInformation("Waiting for the next day to generate certificates");
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}