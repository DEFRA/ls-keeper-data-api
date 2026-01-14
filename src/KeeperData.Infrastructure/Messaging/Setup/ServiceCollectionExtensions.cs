using Amazon.SimpleNotificationService;
using Amazon.SQS;
using KeeperData.Application.MessageHandlers.Sam;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.Serializers;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Messaging.Observers;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Messaging.Throttling;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Consumers;
using KeeperData.Infrastructure.Messaging.Factories;
using KeeperData.Infrastructure.Messaging.Factories.Implementations;
using KeeperData.Infrastructure.Messaging.MessageHandlers;
using KeeperData.Infrastructure.Messaging.Publishers;
using KeeperData.Infrastructure.Messaging.Publishers.Configuration;
using KeeperData.Infrastructure.Messaging.Services;
using KeeperData.Infrastructure.Messaging.Throttling;
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

        services.AddDataImportThrottling(configuration);

        services.AdddMessageSerializers();

        services.AddMessageHandlers();

        services.AddServiceBusSenderDependencies(configuration);

        services.AddBatchCompletionNotificationDependencies(configuration);

        if (!intakeEventQueueOptions.Disabled)
        {
            services.AddHealthChecks()
                .AddCheck<QueueHealthCheck<IntakeEventQueueOptions>>("intake-event-consumer", tags: ["aws", "sqs"])
                .AddCheck<AwsSnsHealthCheck>("batch-completion-publisher", tags: ["aws", "sns"]);
        }
    }

    private static void AddMessageConsumers(this IServiceCollection services)
    {
        services.AddHostedService<QueueListener>()
            .AddSingleton<IQueuePoller, QueuePoller>();
        services.AddSingleton<IDeadLetterQueueService, DeadLetterQueueService>();

        services.AddTransient<IQueuePollerObserver<MessageType>, NullQueuePollerObserver<MessageType>>();
    }

    private static void AddDataImportThrottling(this IServiceCollection services, IConfiguration configuration)
    {
        var dataImportThrottlingConfiguration = configuration.GetSection(DataImportThrottlingConfiguration.SectionName).Get<DataImportThrottlingConfiguration>() ?? new();
        services.AddSingleton<IDataImportThrottlingConfiguration>(dataImportThrottlingConfiguration);
    }

    private static void AdddMessageSerializers(this IServiceCollection services)
    {
        services.AddSingleton<IMessageSerializer<SnsEnvelope>, SnsEnvelopeSerializer>();

        var messageIdentifierTypes = new[]
        {
            typeof(SamBulkScanMessage),
            typeof(SamDailyScanMessage),
            typeof(CtsBulkScanMessage),
            typeof(CtsDailyScanMessage),
            typeof(SamImportHoldingMessage),
            typeof(SamUpdateHoldingMessage),
            typeof(CtsImportHoldingMessage),
            typeof(CtsUpdateHoldingMessage),
            typeof(CtsUpdateKeeperMessage),
            typeof(CtsUpdateAgentMessage),
            typeof(BatchCompletionMessage)
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
        var handlerTypes = typeof(SamImportHoldingMessageHandler).Assembly.GetTypes()
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

        services.AddSingleton(sp =>
        {
            var registry = new MessageCommandRegistry();

            registry.Register<SamBulkScanMessageCommandFactory>("SamBulkScan");
            registry.Register<SamDailyScanMessageCommandFactory>("SamDailyScan");
            registry.Register<CtsBulkScanMessageCommandFactory>("CtsBulkScan");
            registry.Register<CtsDailyScanMessageCommandFactory>("CtsDailyScan");
            registry.Register<SamImportHoldingCommandFactory>("SamImportHolding");
            registry.Register<SamUpdateHoldingMessageCommandFactory>("SamUpdateHolding");
            registry.Register<CtsImportHoldingMessageCommandFactory>("CtsImportHolding");
            registry.Register<CtsUpdateHoldingMessageCommandFactory>("CtsUpdateHolding");
            registry.Register<CtsUpdateKeeperMessageCommandFactory>("CtsUpdateKeeper");
            registry.Register<CtsUpdateAgentMessageCommandFactory>("CtsUpdateAgent");
            registry.Register<BatchCompletionMessageCommandFactory>("BatchCompletion");

            return registry;
        });

        return services;
    }

    private static void AddServiceBusSenderDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceBusSenderConfiguration = configuration.GetSection(nameof(ServiceBusSenderConfiguration)).Get<ServiceBusSenderConfiguration>()!;
        services.AddSingleton<IServiceBusSenderConfiguration>(serviceBusSenderConfiguration);

        services.AddServiceBusEventPublishers();
    }

    private static IServiceCollection AddServiceBusEventPublishers(this IServiceCollection services)
    {
        services.AddTransient<IMessageFactory, MessageFactory>();

        services.AddSingleton<IMessagePublisher<IntakeEventsQueueClient>, IntakeEventQueuePublisher>();

        return services;
    }

    private static void AddBatchCompletionNotificationDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var batchCompletionConfig = configuration.GetSection(nameof(BatchCompletionNotificationConfiguration)).Get<BatchCompletionNotificationConfiguration>() ?? new();
        services.AddSingleton<IBatchCompletionNotificationConfiguration>(batchCompletionConfig);

        if (configuration["LOCALSTACK_ENDPOINT"] != null)
        {
            services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
            {
                var config = new AmazonSimpleNotificationServiceConfig
                {
                    ServiceURL = configuration["AWS:ServiceURL"],
                    AuthenticationRegion = configuration["AWS:Region"],
                    UseHttp = true
                };
                var credentials = new Amazon.Runtime.BasicAWSCredentials("test", "test");
                return new AmazonSimpleNotificationServiceClient(credentials, config);
            });
        }
        else
        {
            services.AddAWSService<IAmazonSimpleNotificationService>();
        }

        services.AddSingleton<IMessagePublisher<BatchCompletionTopicClient>, BatchCompletionTopicPublisher>();
    }
}