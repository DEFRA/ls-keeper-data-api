using KeeperData.Infrastructure.Authentication.Configuration;
using KeeperData.Infrastructure.Authentication.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Authentication.Setup;

public static class ServiceCollectionExtensions
{
    public static void AddAuthenticationDependencies(this IServiceCollection services)
    {
        services.AddOptions<AclOptions>()
            .BindConfiguration("Acl")
            .ValidateDataAnnotations();

        services.AddOptions<AuthenticationConfiguration>()
            .BindConfiguration(nameof(AuthenticationConfiguration))
            .ValidateDataAnnotations();

        var authBuilder = services.AddAuthentication();

        authBuilder.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
            BasicAuthenticationHandler.SchemeName, _ => { });

        authBuilder.AddJwtBearer("Bearer", _ => { });

        services.AddSingleton<IConfigureOptions<AuthenticationOptions>, AuthenticationOptionsConfigurator>();

        services.AddSingleton<IConfigureNamedOptions<JwtBearerOptions>, JwtBearerOptionsConfigurator>();
    }
}
