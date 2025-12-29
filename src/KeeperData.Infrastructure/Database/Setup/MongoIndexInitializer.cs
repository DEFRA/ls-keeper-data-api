using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KeeperData.Infrastructure.Database.Setup;

public class MongoIndexInitializer(IServiceProvider serviceProvider) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureMongoIndexesAsync();
    }

    public async Task EnsureMongoIndexesAsync()
    {
        var dbInitialiser = _serviceProvider.GetService<IMongoDbInitialiser>();

        var indexableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(IContainsIndexes).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in indexableTypes)
        {
            await dbInitialiser!.Initialise(type);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}