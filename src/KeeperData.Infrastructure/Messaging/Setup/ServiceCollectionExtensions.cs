using Amazon.SQS;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Infrastructure.Messaging.Setup;

public static class ServiceCollectionExtensions
{
    public static void AddMessagingDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var intakeEventQueueConfig = configuration.GetRequiredSection($"{nameof(QueueConsumerOptions)}:{nameof(IntakeEventQueueOptions)}");
        services.Configure<IntakeEventQueueOptions>(intakeEventQueueConfig);
        services.AddSingleton(intakeEventQueueConfig.Get<IntakeEventQueueOptions>()!);

        services.AddAWSService<IAmazonSQS>();

        services.AddServiceBusEventConsumers();

        services.AddHealthChecks()
            .AddCheck<QueueHealthCheck<IntakeEventQueueOptions>>("intake-event-consumer", tags: ["aws", "sqs"]);
    }

    private static void AddServiceBusEventConsumers(this IServiceCollection services)
    {
        services.AddHostedService<IntakeEventConsumer>();
    }
}