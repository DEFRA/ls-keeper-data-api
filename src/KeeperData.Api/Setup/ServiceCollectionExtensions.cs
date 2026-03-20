using KeeperData.Api.Utils;
using KeeperData.Api.Worker.Setup;
using KeeperData.Application.Configuration;
using KeeperData.Application.Setup;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Infrastructure.ApiClients;
using KeeperData.Infrastructure.ApiClients.Decorators;
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
using Scrutor;

namespace KeeperData.Api.Setup;

public static class ServiceCollectionExtensions
{
    public static void ConfigureApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
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

        services.ConfigureSwagger();

        services.ConfigureHealthChecks();

        services.AddApplicationLayer(configuration);

        services.AddDatabaseDependencies(configuration);

        services.AddMessagingDependencies(configuration);

        services.AddStorageDependencies(configuration);

        services.AddApiClientDependencies(configuration);

        services.AddBackgroundJobDependencies(configuration);

        services.AddKeeperDataMetrics(configuration);

        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(MetricNames.MeterName);
            });

        services.ConfigurePiiAnonymization(configuration);
    }

    private static void ConfigurePiiAnonymization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PiiAnonymizationOptions.SectionName)
            .Get<PiiAnonymizationOptions>();

        if (options?.Enabled != true) return;

        services.Decorate<IDataBridgeClient, DataBridgeClientAnonymizer>();
    }

    private static readonly string ApiDescription = """
        The Livestock Keeper Data API is a reference data service that allows developers to build applications that connect with Defra's Livestock - Location and Party Data domain.

        With the Livestock Keeper Data API, you can:

        * **Authenticate** with Defra's identity system using Bearer (JWT) or Basic authentication.
        * **Retrieve** location-based reference data such as Sites and its related data such as Parties, Species, Marks etc.
        * **Retrieve** reference data for Parties and related information.
        * **Trigger** data import scans (internal, feature-gated).
        * **Manage** dead letter queue messages (internal, feature-gated).

        All list endpoints support **Change Data Capture (CDC)** via the `lastUpdatedDate` query parameter, returning only records updated since the provided timestamp.

        All list endpoints return paginated results with `count`, `totalCount`, `values`, `page`, `pageSize`, `totalPages`, `hasNextPage`, and `hasPreviousPage` fields.
        """;

    private static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            var contactInfo = new OpenApiContact
            {
                Name = "Defra Livestock Data Services Support",
                Url = new Uri("https://www.defra.gov.uk/support"),
                Email = "support_cdp_platform@defra.gov.uk"
            };

            var licenseInfo = new OpenApiLicense
            {
                Name = "Livestock Data Services Agreement",
                Url = new Uri("https://www.defra.gov.uk/services-agreement/")
            };

            options.SwaggerDoc("public", new OpenApiInfo
            {
                Title = "Livestock Keeper Data API (Public)",
                Version = "1.0.0",
                Description = ApiDescription,
                TermsOfService = new Uri("https://www.defra.gov.uk/legal"),
                Contact = contactInfo,
                License = licenseInfo
            });

            options.SwaggerDoc("internal", new OpenApiInfo
            {
                Title = "Livestock Keeper Data API (Internal)",
                Version = "1.0.0",
                Description = "Internal endpoints for data ingestion, scanning, and administration.",
                Contact = contactInfo,
                License = licenseInfo
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT authorisation token",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                Description = "Basic authentication credentials",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "basic"
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

            // Include XML comments from all relevant assemblies
            var assemblies = new[]
            {
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                "KeeperData.Core",
                "KeeperData.Application"
            };

            foreach (var assemblyName in assemblies)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            }
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