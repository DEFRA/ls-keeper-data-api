using Amazon.SQS;
using KeeperData.Application.MessageHandlers;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.Serializers;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.Contracts.V1.Serializers;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Consumers;
using KeeperData.Infrastructure.Messaging.MessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Infrastructure.Messaging.Setup;

public static class ServiceCollectionExtensions
{
    public static void AddMessagingDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var intakeEventQueueConfig = configuration.GetRequiredSection($"{nameof(QueueConsumerOptions)}:{nameof(IntakeEventQueueOptions)}");
        services.Configure<IntakeEventQueueOptions>(intakeEventQueueConfig);

        var intakeEventQueueOptions = intakeEventQueueConfig.Get<IntakeEventQueueOptions>() ?? new() { QueueUrl = "Missing", Disabled = true };
        services.AddSingleton(intakeEventQueueOptions);

        if (configuration["LOCALSTACK_ENDPOINT"] != null)
        {
            services.AddSingleton<IAmazonSQS>(sp =>
            {
                var config = new AmazonSQSConfig
                {
                    ServiceURL = configuration["AWS:ServiceURL"],
                    AuthenticationRegion = configuration["AWS:Region"],
                    UseHttp = true
                };
                var credentials = new Amazon.Runtime.BasicAWSCredentials("test", "test");
                return new AmazonSQSClient(credentials, config);
            });
        }
        else
        {
            services.AddAWSService<IAmazonSQS>();
        }

        services.AddMessageConsumers();

        services.AdddMessageSerializers();

        services.AddMessageHandlers();

        if (!intakeEventQueueOptions.Disabled)
        {
            services.AddHealthChecks()
                .AddCheck<QueueHealthCheck<IntakeEventQueueOptions>>("intake-event-consumer", tags: ["aws", "sqs"]);
        }
    }

    private static void AddMessageConsumers(this IServiceCollection services)
    {
        services.AddHostedService<QueueListener>()
            .AddSingleton<IQueuePoller, QueuePoller>();
    }

    private static void AdddMessageSerializers(this IServiceCollection services)
    {
        services.AddSingleton<IMessageSerializer<SnsEnvelope>, SnsEnvelopeSerializer>();

        services.AddSingleton<IUnwrappedMessageSerializer<PlaceholderMessage>, PlaceholderMessageSerializer>();
    }

    private static IServiceCollection AddMessageHandlers(this IServiceCollection services)
    {
        services.AddTransient<IMessageHandler<PlaceholderMessage>, PlaceholderMessageHandler>();

        var messageHandlerManager = new InMemoryMessageHandlerManager();
        messageHandlerManager.AddReceiver<PlaceholderMessage, IMessageHandler<PlaceholderMessage>>();

        services.AddSingleton<IMessageHandlerManager>(messageHandlerManager);

        return services;
    }
}