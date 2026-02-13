using KeeperData.Api.Utils;
using KeeperData.Api.Worker.Setup;
using KeeperData.Application.Setup;
using KeeperData.Core.Telemetry;
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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
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

        services.ConfigureSwagger(); // Add this line

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

    private static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Keeper Data API",
                Version = "v1",
                Description = "API for managing and accessing Keeper Data services. This API provides endpoints for data management, authentication, and integration with various backend services."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                Description = "Basic Authentication header. Example: \"Authorization: Basic {base64(username:password)}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Basic"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Basic"
                        }
                    },
                    Array.Empty<string>()
                }
            });
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
        services.AddSingleton<IConfigureNamedOptions<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>, JwtBearerOptionsConfigurator>();

        var authBuilder = services.AddAuthentication();

        if (authConfig.EnableApiKey)
        {
            authBuilder.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName, _ => { });
        }

        if (authConfig.ApiGatewayExists)
        {
            authBuilder.AddJwtBearer("Bearer", (options) =>
            {
                options.Authority = authConfig.Authority;
                options.TokenValidationParameters.ValidateAudience = false;
                options.BackchannelHttpHandler = new ProxyHttpMessageHandler();
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