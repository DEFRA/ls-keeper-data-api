using Microsoft.Extensions.Hosting;

namespace KeeperData.Infrastructure.Database.Setup;

public class MongoIndexInitializer(IServiceProvider serviceProvider) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ServiceCollectionExtensions.EnsureMongoIndexesAsync(_serviceProvider);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}