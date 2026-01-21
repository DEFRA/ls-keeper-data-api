using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KeeperData.Infrastructure.Authentication.Configuration;

public class JwtBearerOptionsConfigurator(IOptions<AuthenticationConfiguration> authConfig) : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly AuthenticationConfiguration _authConfig = authConfig.Value;

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != "Bearer")
            return;

        if (_authConfig.ApiGatewayExists)
        {
            if (string.IsNullOrWhiteSpace(_authConfig.Authority))
                throw new InvalidOperationException("Authority must be configured when ApiGatewayExists = true");

            options.Authority = _authConfig.Authority;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        }
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}