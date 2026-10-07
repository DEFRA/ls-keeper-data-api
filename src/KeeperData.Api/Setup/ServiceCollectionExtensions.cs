using KeeperData.Api.Utils;
using KeeperData.Application.Configuration;
using KeeperData.Application.Setup;
using KeeperData.Core.Telemetry;
using KeeperData.Infrastructure.ApiClients.Setup;
using KeeperData.Infrastructure.Authentication.Configuration;
using KeeperData.Infrastructure.Authentication.Handlers;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Database.Setup;
using KeeperData.Infrastructure.Extensions;
using KeeperData.Infrastructure.Storage.Setup;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

namespace KeeperData.Api.Setup;

public static class ServiceCollectionExtensions
{
    public static void ConfigureOpenApiGeneration(this IServiceCollection services)
    {
        services.ConfigureControllers();
        services.ConfigureOpenApi();
    }

    public static void ConfigureApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        EnsureFakeClientsAreDevelopmentOnly(configuration, environment);

        services.ConfigureAuthentication(configuration);

        services.ConfigureControllers();

        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.Configure<AwsConfig>(configuration.GetSection(AwsConfig.SectionName));

        services.ConfigureOpenApi();
        services.ConfigureSwagger();

        services.ConfigureHealthChecks();

        services.AddApplicationLayer(configuration);

        services.AddDatabaseDependencies(configuration);

        services.AddStorageDependencies(configuration);

        services.AddApiClientDependencies(configuration);

        services.AddKeeperDataMetrics(configuration);

        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(MetricNames.MeterName);
            });
    }

    private static void EnsureFakeClientsAreDevelopmentOnly(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (configuration.GetValue<bool>("ApiClients:DataBridgeApi:UseFakeClient") &&
            !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "ApiClients:DataBridgeApi:UseFakeClient can only be enabled in the Development environment.");
        }
    }

    private static void ConfigureControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(Program).Assembly)
            .AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });
    }

    private static void ConfigureOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v2", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.ShouldInclude = description => description.GroupName == "v2";
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Livestock Keeper Data API (V2)",
                    Version = "2.0.0",
                    Description = V2ApiDescription,
                    TermsOfService = new Uri("https://www.defra.gov.uk/legal"),
                    Contact = new OpenApiContact
                    {
                        Name = "Defra Livestock Data Services Support",
                        Url = new Uri("https://www.defra.gov.uk/support"),
                        Email = "support_cdp_platform@defra.gov.uk"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Livestock Data Services Agreement",
                        Url = new Uri("https://www.defra.gov.uk/services-agreement/")
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Description = "JWT authorisation token",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    },
                    ["Basic"] = new OpenApiSecurityScheme
                    {
                        Description = "Basic authentication credentials",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "basic"
                    }
                };
                document.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    },
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Basic", document)] = []
                    }
                ];

                return Task.CompletedTask;
            });
        });
    }

    private static readonly string ApiDescription = """
        The Livestock Keeper Data API is a reference data service that allows developers to build applications that connect with Defra's Livestock - Location and Party Data domain.

        With the Livestock Keeper Data API, you can:

        * **Authenticate** with Defra's identity system using Bearer (JWT) or Basic authentication.
        * **Retrieve** location-based reference data such as Sites and its related data such as Parties, Species, Marks etc.
        * **Retrieve** reference data for Parties and related information.

        **Note:** the endpoints in this document are retired and disabled via `LegacyEndpointsEnabled`. They are retained in the codebase but serve stale data only; consumers should migrate to the V2 API backed by the new data ingest process.

        All list endpoints support **Change Data Capture (CDC)** via the `lastUpdatedDate` query parameter, returning only records updated since the provided timestamp.

        All list endpoints return paginated results with `count`, `totalCount`, `values`, `page`, `pageSize`, `totalPages`, `hasNextPage`, and `hasPreviousPage` fields.
        """;

    private static readonly string V2ApiDescription = """
        Version 2 of the Livestock Keeper Data API provides authenticated access to user accounts, their CPH associations, and holding details.
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

            options.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Livestock Keeper Data API (V2)",
                Version = "2.0.0",
                Description = V2ApiDescription,
                TermsOfService = new Uri("https://www.defra.gov.uk/legal"),
                Contact = contactInfo,
                License = licenseInfo
            });

            options.SwaggerDoc("internal", new OpenApiInfo
            {
                Title = "Livestock Keeper Data API (Internal)",
                Version = "1.0.0",
                Description = "Internal endpoints for cache administration.",
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

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    []
                },
                {
                    new OpenApiSecuritySchemeReference("Basic", document),
                    []
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
            })
            .AddPolicy("AdminScopeOrApiKey", policy =>
            {
                if (authConfig.EnableApiKey) policy.AddAuthenticationSchemes("Basic");
                if (authConfig.ApiGatewayExists) policy.AddAuthenticationSchemes("Bearer");
                policy.RequireAssertion(context => context.User.Identities.Any(identity =>
                    identity.IsAuthenticated && (identity.AuthenticationType == "Basic" ||
                    identity.Claims.Any(claim => claim.Type == "scope" &&
                        claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("admin", StringComparer.OrdinalIgnoreCase)))));
            });
    }

    private static void ConfigureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        services.AddSingleton<IHealthCheckPublisher, HealthCheckMetricsPublisher>();
    }
}