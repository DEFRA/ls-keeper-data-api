using KeeperData.Core.Services;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Sources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Infrastructure.Storage.Setup;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    /// <summary>The read model runs to hundreds of megabytes, so it needs longer than the default 100s.</summary>
    private static readonly TimeSpan SqliteArtifactDownloadTimeout = TimeSpan.FromMinutes(10);

    public static void AddStorageDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("ApiClients:DataBridgeApi:UseFakeClient"))
        {
            services.AddSingleton<ISqliteArtifactSource, FakeSqliteArtifactSource>();
        }
        else
        {
            services.AddHttpClient(DataBridgeSqliteArtifactSource.DownloadClientName, client =>
                {
                    client.Timeout = SqliteArtifactDownloadTimeout;
                })
                .ConfigurePrimaryHttpMessageHandler(() => new SqliteArtifactDownloadHandler());

            services.AddSingleton<ISqliteArtifactSource, DataBridgeSqliteArtifactSource>();
        }

        var cphCacheConfig = configuration
            .GetSection(CphSqliteCacheConfiguration.SectionName)
            .Get<CphSqliteCacheConfiguration>() ?? new CphSqliteCacheConfiguration();
        services.AddSingleton(cphCacheConfig);

        services.AddSingleton<CphSqliteCacheService>();
        services.AddSingleton<ICphSqliteCacheService>(sp => sp.GetRequiredService<CphSqliteCacheService>());
        services.AddHostedService(sp => sp.GetRequiredService<CphSqliteCacheService>());

        var readModelCacheConfig = configuration
            .GetSection(ReadModelSqliteCacheConfiguration.SectionName)
            .Get<ReadModelSqliteCacheConfiguration>() ?? new ReadModelSqliteCacheConfiguration();
        services.AddSingleton(readModelCacheConfig);

        services.AddSingleton<ReadModelSqliteCacheService>();
        services.AddSingleton<IReadModelSqliteCacheService>(sp => sp.GetRequiredService<ReadModelSqliteCacheService>());
        services.AddHostedService(sp => sp.GetRequiredService<ReadModelSqliteCacheService>());
    }
}