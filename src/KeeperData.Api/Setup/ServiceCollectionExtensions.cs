using KeeperData.Application.Setup;
using KeeperData.Infrastructure.ApiClients.Setup;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Database.Setup;
using KeeperData.Infrastructure.Extensions;
using KeeperData.Infrastructure.Messaging.Setup;
using KeeperData.Infrastructure.Storage.Setup;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;
using KeeperData.Api.Worker.Setup;

namespace KeeperData.Api.Setup;

public static class ServiceCollectionExtensions
{
    public static void ConfigureApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });

        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.Configure<AwsConfig>(configuration.GetSection(AwsConfig.SectionName));

        services.ConfigureHealthChecks();

        services.AddApplicationLayer();

        services.AddDatabaseDependencies(configuration);

        services.AddMessagingDependencies(configuration);

        services.AddStorageDependencies(configuration);

        services.AddApiClientDependencies(configuration);

        services.AddBackgroundJobDependencies(configuration);

        services.AddKeeperDataMetrics();

        // Configure OpenTelemetry for metrics
        services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(MetricNames.MeterName);
            });
    }

    private static void ConfigureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        services.AddSingleton<IHealthCheckPublisher, HealthCheckMetricsPublisher>();
    }
}