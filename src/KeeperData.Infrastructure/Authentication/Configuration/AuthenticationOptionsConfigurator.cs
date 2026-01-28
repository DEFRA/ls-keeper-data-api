using KeeperData.Infrastructure.Authentication.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Authentication.Configuration;

public class AuthenticationOptionsConfigurator(IOptions<AuthenticationConfiguration> authConfig) : IConfigureOptions<AuthenticationOptions>
{
    private readonly AuthenticationConfiguration _authConfig = authConfig.Value;

    public void Configure(AuthenticationOptions options)
    {
        var scheme = _authConfig.ApiGatewayExists
            ? "Bearer"
            : BasicAuthenticationHandler.SchemeName;

        options.DefaultScheme = scheme;
        options.DefaultAuthenticateScheme = scheme;
        options.DefaultChallengeScheme = scheme;
    }
}