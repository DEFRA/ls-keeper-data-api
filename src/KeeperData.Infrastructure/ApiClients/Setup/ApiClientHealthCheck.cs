using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KeeperData.Infrastructure.ApiClients.Setup;

public class ApiClientHealthCheck(
    IHttpClientFactory httpClientFactory,
    string clientName,
    string healthEndpoint = "/health",
    int timeoutSeconds = 10) : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly string _clientName = clientName;
    private readonly string _healthEndpoint = healthEndpoint;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(timeoutSeconds);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        Exception? exception = null;
        HttpResponseMessage? response = null;

        try
        {
            var client = _httpClientFactory.CreateClient(_clientName);
            response = await client.GetAsync(_healthEndpoint, cts.Token);
        }
        catch (TaskCanceledException)
        {
            exception = new TimeoutException($"Health check for '{_clientName}' timed out after {_timeout.TotalSeconds} seconds.");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var status = response?.IsSuccessStatusCode == true
            ? HealthStatus.Healthy
            : response != null
                ? HealthStatus.Degraded
                : HealthStatus.Unhealthy;

        var data = new Dictionary<string, object>
        {
            { "client-name", _clientName },
            { "endpoint", _healthEndpoint },
            { "status-code", response?.StatusCode ?? System.Net.HttpStatusCode.Unused },
            { "reason", response?.ReasonPhrase ?? string.Empty }
        };

        if (exception != null)
        {
            data["error"] = $"{exception.Message} - {exception.InnerException?.Message}";
        }

        return new HealthCheckResult(
            status: status,
            description: $"Health check for HTTP client '{_clientName}'",
            exception: exception,
            data: data);
    }
}