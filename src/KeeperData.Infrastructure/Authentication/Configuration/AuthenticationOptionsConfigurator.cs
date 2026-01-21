using KeeperData.Infrastructure.Authentication.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Authentication.Configuration;

public class AuthenticationOptionsConfigurator(IOptions<AuthenticationConfiguration> authConfig) : IConfigureOptions<AuthenticationOptions>
{
    private readonly AuthenticationConfiguration _authConfig = authConfig.Value;

    public void Configure(AuthenticationOptions options)
    {
        options.DefaultAuthenticateScheme =
            _authConfig.ApiGatewayExists ? "Bearer" : BasicAuthenticationHandler.SchemeName;

        options.DefaultChallengeScheme = options.DefaultAuthenticateScheme;
    }
}