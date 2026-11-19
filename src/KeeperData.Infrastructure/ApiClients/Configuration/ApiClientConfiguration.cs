namespace KeeperData.Infrastructure.ApiClients.Configuration;

public class ApiClientConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string BridgeApiSubscriptionKey { get; set; } = string.Empty;
    public bool UseStsAuthentication { get; set; } = false;
    public string StsEndpoint { get; set; } = "https://sts.eu-west-2.amazonaws.com";
    public string StsRegion { get; set; } = "eu-west-2";
    public int StsExpiresSeconds { get; set; } = 60;
    public string ServiceName { get; set; } = string.Empty;
    public string XApiKey { get; set; } = string.Empty;
    public bool HealthcheckEnabled { get; set; } = false;
    public ResiliencePolicy ResiliencePolicy { get; set; } = new();
}