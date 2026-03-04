using Amazon.CloudWatch;
using KeeperData.Core.Telemetry;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class TelemetryExtensions
{
    public static IServiceCollection AddKeeperDataMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IApplicationMetrics, ApplicationMetrics>();
        services.TryAddSingleton<HealthCheckMetrics>();
        services.TryAddSingleton<HealthCheckMetricsPublisher>();

        if (!string.IsNullOrWhiteSpace(configuration["LOCALSTACK_ENDPOINT"]))
        {
            services.AddSingleton<IAmazonCloudWatch>(sp =>
            {
                var config = new AmazonCloudWatchConfig
                {
                    ServiceURL = configuration["AWS:ServiceURL"],
                    AuthenticationRegion = configuration["AWS:Region"],
                    UseHttp = true
                };
                var credentials = new Amazon.Runtime.BasicAWSCredentials("test", "test");
                return new AmazonCloudWatchClient(credentials, config);
            });
        }

        return services;
    }
}