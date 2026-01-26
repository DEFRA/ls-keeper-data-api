using KeeperData.Api.Worker.Setup;
using KeeperData.Application.Setup;
using KeeperData.Infrastructure.ApiClients.Setup;
using KeeperData.Infrastructure.Authentication.Configuration;
using KeeperData.Infrastructure.Authentication.Handlers;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Database.Setup;
using KeeperData.Infrastructure.Extensions;
using KeeperData.Infrastructure.Messaging.Setup;
using KeeperData.Infrastructure.Storage.Setup;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace KeeperData.Api.Setup;

public static class ServiceCollectionExtensions
{
    public static void ConfigureApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureAuthentication(configuration);

        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });

        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.Configure<AwsConfig>(configuration.GetSection(AwsConfig.SectionName));

        services.ConfigureHealthChecks();

        services.AddApplicationLayer(configuration);

        services.AddDatabaseDependencies(configuration);

        services.AddMessagingDependencies(configuration);

        services.AddStorageDependencies(configuration);

        services.AddApiClientDependencies(configuration);

        services.AddBackgroundJobDependencies(configuration);

        services.AddKeeperDataMetrics();

        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(MetricNames.MeterName);
            });
    }

    public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authConfig = configuration.GetSection(nameof(AuthenticationConfiguration)).Get<AuthenticationConfiguration>()!;

        services.Configure<AclOptions>(
            configuration.GetSection("Acl"));

        services.Configure<AuthenticationConfiguration>(
            configuration.GetSection("AuthenticationConfiguration"));

        services.AddSingleton<IConfigureOptions<AuthenticationOptions>, AuthenticationOptionsConfigurator>();
        services.AddSingleton<IConfigureNamedOptions<JwtBearerOptions>, JwtBearerOptionsConfigurator>();

        var authBuilder = services.AddAuthentication();

        if (authConfig.EnableApiKey)
        {
            authBuilder.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName, _ => { });
        }

        if (authConfig.ApiGatewayExists)
        {
            authBuilder.AddJwtBearer("Bearer", options =>
            {
                options.Authority = authConfig.Authority;
                options.TokenValidationParameters.ValidateAudience = false;
            });
        }

        services.AddAuthorizationBuilder()
            .AddPolicy("BasicOrBearer", policy =>
            {
                if (authConfig.EnableApiKey)
                {
                    policy.AddAuthenticationSchemes("Basic");
                }
                if (authConfig.ApiGatewayExists)
                {
                    policy.AddAuthenticationSchemes("Bearer");
                }
                policy.RequireAuthenticatedUser();
            });
    }

    private static void ConfigureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        services.AddSingleton<IHealthCheckPublisher, HealthCheckMetricsPublisher>();
    }
}