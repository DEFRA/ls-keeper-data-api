namespace KeeperData.Infrastructure.Authentication.Configuration;

public class AuthenticationConfiguration
{
    public bool EnableApiKey { get; set; }
    public bool ApiGatewayExists { get; set; }
    public string? Authority { get; set; }
}