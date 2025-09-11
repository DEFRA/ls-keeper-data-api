using Amazon.SQS;
using KeeperData.Core.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddQueueConsumers(this IServiceCollection services, IConfiguration config)
    {
        services.AddAWSService<IAmazonSQS>();

        services.AddSingleton<IIntakeEventRepository, IntakeEventRepository>();
        services.AddHostedService<IntakeEventConsumer>()
            .Configure<IntakeEventConsumerOptions>(config.GetRequiredSection("QueueConsumerConfiguration:IntakeEventQueue"));
        return services;
    }
}