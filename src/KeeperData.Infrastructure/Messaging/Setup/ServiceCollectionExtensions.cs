using Amazon.SQS;
using KeeperData.Application.MessageHandlers.Sam;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.Serializers;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
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

        var messageIdentifierTypes = new[]
        {
            typeof(SamHoldingInsertedMessage),
            typeof(SamHoldingUpdatedMessage),
            typeof(SamHolderUpdatedMessage),
            typeof(SamPartyUpdatedMessage),
            typeof(SamHoldingDeletedMessage),
            typeof(SamHolderDeletedMessage),
            typeof(SamPartyDeletedMessage),
            typeof(CtsHoldingInsertedMessage),
            typeof(CtsHoldingUpdatedMessage),
            typeof(CtsAgentUpdatedMessage),
            typeof(CtsKeeperUpdatedMessage),
            typeof(CtsHoldingDeletedMessage),
            typeof(CtsAgentDeletedMessage),
            typeof(CtsKeeperDeletedMessage)
        };

        foreach (var messageType in messageIdentifierTypes)
        {
            var typeInfo = MessageIdentifierSerializerContext.Default.GetType().GetProperty(messageType.Name)?.GetValue(MessageIdentifierSerializerContext.Default);

            var serializerType = typeof(MessageIdentifierSerializer<>).MakeGenericType(messageType);
            var interfaceType = typeof(IUnwrappedMessageSerializer<>).MakeGenericType(messageType);

            services.AddSingleton(interfaceType, Activator.CreateInstance(serializerType, typeInfo)!);
        }
    }

    private static IServiceCollection AddMessageHandlers(this IServiceCollection services)
    {
        var handlerInterfaceType = typeof(IMessageHandler<>);
        var handlerTypes = typeof(SamHoldingInsertedMessageHandler).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Select(type => new
            {
                Implementation = type,
                Interface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
            })
            .Where(x => x.Interface != null);

        var messageHandlerManager = new InMemoryMessageHandlerManager();

        foreach (var types in handlerTypes)
        {
            services.AddTransient(types.Interface!, types.Implementation);

            var messageType = types.Interface!.GenericTypeArguments[0];
            var addReceiverMethod = typeof(InMemoryMessageHandlerManager)
                .GetMethod(nameof(InMemoryMessageHandlerManager.AddReceiver))!
                .MakeGenericMethod(messageType, types.Interface);

            addReceiverMethod.Invoke(messageHandlerManager, null);
        }

        services.AddSingleton<IMessageHandlerManager>(messageHandlerManager);

        return services;
    }
}