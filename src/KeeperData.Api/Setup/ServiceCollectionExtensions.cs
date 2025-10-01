using KeeperData.Application.Setup;
using KeeperData.Infrastructure.ApiClients.Setup;
using KeeperData.Infrastructure.Database.Setup;
using KeeperData.Infrastructure.Messaging.Setup;
using KeeperData.Infrastructure.Storage.Setup;
using System.Text.Json.Serialization;

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

        services.ConfigureHealthChecks();

        services.AddApplicationLayer();

        services.AddDatabaseDependencies(configuration);

        services.AddMessagingDependencies(configuration);

        services.AddStorageDependencies(configuration);

        services.AddApiClientDependencies(configuration);
    }

    private static void ConfigureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
    }
}