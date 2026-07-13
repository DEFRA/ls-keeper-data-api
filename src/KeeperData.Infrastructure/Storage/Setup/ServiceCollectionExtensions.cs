using Amazon;
using Amazon.S3;
using KeeperData.Core.Services;
using KeeperData.Core.Storage;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Factories;
using KeeperData.Infrastructure.Storage.Factories.Implementations;
using KeeperData.Infrastructure.Storage.Readers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Infrastructure.Storage.Setup;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddStorageDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var storageConfiguration = configuration.GetSection(nameof(StorageConfiguration)).Get<StorageConfiguration>()!;
        services.AddSingleton(storageConfiguration);

        var defaultAmazonS3Config = GetDefaultAmazonS3Config(configuration);
        services.AddSingleton(defaultAmazonS3Config);

        var factory = new S3ClientFactory();

        factory.AddClient<ComparisonReportsStorageClient>(
            storageConfiguration.ComparisonReportsStorage.BucketName,
            defaultAmazonS3Config);

        if (storageConfiguration.ComparisonReportsStorage.HealthcheckEnabled)
        {
            services.AddHealthChecks()
                .AddCheck<AwsS3HealthCheck>("aws_s3", tags: ["aws", "s3"]);
        }

        services.AddSingleton<IS3ClientFactory>(factory);

        services.AddTransient<IStorageReader<ComparisonReportsStorageClient>, ComparisonReportsStorageReader>();

        if (!string.IsNullOrWhiteSpace(storageConfiguration.CphSqliteStorage.BucketName) &&
            !storageConfiguration.CphSqliteStorage.BucketName.StartsWith("Set in"))
        {
            factory.AddClient<CphSqliteStorageClient>(
                storageConfiguration.CphSqliteStorage.BucketName,
                defaultAmazonS3Config);
        }

        var cphCacheConfig = configuration
            .GetSection(CphSqliteCacheConfiguration.SectionName)
            .Get<CphSqliteCacheConfiguration>() ?? new CphSqliteCacheConfiguration();
        services.AddSingleton(cphCacheConfig);

        services.AddSingleton<CphSqliteCacheService>();
        services.AddSingleton<ICphSqliteCacheService>(sp => sp.GetRequiredService<CphSqliteCacheService>());
        services.AddHostedService(sp => sp.GetRequiredService<CphSqliteCacheService>());
    }

    private static AmazonS3Config GetDefaultAmazonS3Config(IConfiguration configuration)
    {
        if (configuration["LOCALSTACK_ENDPOINT"] != null)
        {
            return new AmazonS3Config
            {
                ServiceURL = configuration["LOCALSTACK_ENDPOINT"],
                ForcePathStyle = true
            };
        }

        return new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.EUWest2
        };
    }
}