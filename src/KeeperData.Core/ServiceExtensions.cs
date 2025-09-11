using Amazon.SQS;
using KeeperData.Core.Consumers;
using KeeperData.Core.Health;
using KeeperData.Infrastructure.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KeeperData.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddQueueConsumers(this IServiceCollection services, IConfiguration config)
    {
        services.AddAWSService<IAmazonSQS>();

        var intakeEventConfig = config.GetRequiredSection($"{nameof(QueueConsumerOptions)}:{nameof(IntakeEventQueueOptions)}");
        services.AddSingleton(intakeEventConfig.Get<IntakeEventQueueOptions>()!);
        
        services.AddSingleton<IIntakeEventRepository, IntakeEventRepository>();
        services.AddHostedService<IntakeEventConsumer>()
            .Configure<IntakeEventQueueOptions>(intakeEventConfig);
        return services;
    }
    
    public static IServiceCollection AddCoreRepositories(this IServiceCollection services)
    {
        services.AddSingleton<IIntakeEventRepository, IntakeEventRepository>();

        return services;
    }
}