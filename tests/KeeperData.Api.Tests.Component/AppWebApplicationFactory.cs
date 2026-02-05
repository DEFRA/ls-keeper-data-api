using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Api.Tests.Component.Authentication.Fakes;
using KeeperData.Api.Tests.Component.Consumers.Helpers;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Observers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Infrastructure.Messaging.Consumers;
using KeeperData.Infrastructure.Messaging.Services;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Factories;
using KeeperData.Infrastructure.Storage.Factories.Implementations;
using KeeperData.Core.Locking;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Moq.Protected;
using System.Net;

namespace KeeperData.Api.Tests.Component;

public class AppWebApplicationFactory(
    IDictionary<string, string?>? configurationOverrides = null,
    bool useFakeAuth = false) : WebApplicationFactory<Program>
{
    public Mock<IAmazonS3>? AmazonS3Mock;
    public Mock<IAmazonSQS>? AmazonSQSMock;
    public Mock<IAmazonSimpleNotificationService>? AmazonSNSMock;
    public Mock<IMongoClient>? MongoClientMock;
    public readonly Mock<HttpMessageHandler> DataBridgeApiClientHttpMessageHandlerMock = new();

    public readonly Mock<IDistributedLock> DistributedLockMock = new();
    private readonly HashSet<string> _activeLocks = new();
    private readonly Dictionary<string, Mock<IDistributedLockHandle>> _lockHandles = new();

    public readonly Mock<ISitesRepository> _sitesRepositoryMock = new();
    public readonly Mock<IPartiesRepository> _partiesRepositoryMock = new();
    public readonly Mock<IGenericRepository<CtsHoldingDocument>> _silverCtsHoldingRepositoryMock = new();
    public readonly Mock<IGenericRepository<CtsPartyDocument>> _silverCtsPartyRepositoryMock = new();
    public readonly Mock<IGenericRepository<SamHoldingDocument>> _silverSamHoldingRepositoryMock = new();
    public readonly Mock<IGenericRepository<SamPartyDocument>> _silverSamPartyRepositoryMock = new();
    public readonly Mock<IGenericRepository<SamHerdDocument>> _silverSamHerdRepositoryMock = new();
    public readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    public readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    public readonly Mock<IGoldSitePartyRoleRelationshipRepository> _goldSitePartyRoleRelationshipRepositoryMock = new();
    public readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    public readonly Mock<ICountryRepository> _countryRepositoryMock = new();

    public readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    public readonly Mock<IPremiseActivityTypeLookupService> _premiseActivityTypeLookupServiceMock = new();
    public readonly Mock<IActivityCodeLookupService> _activityCodeLookupServiceMock = new();
    public readonly Mock<IPremiseTypeLookupService> _premiseTypeLookupServiceMock = new();
    public readonly Mock<IProductionTypeLookupService> _productionTypeLookupServiceMock = new();
    public readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    public readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    public readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();
    public readonly Mock<ISiteIdentifierTypeLookupService> _siteIdentifierTypeLookupServiceMock = new();

    public readonly Mock<IRequestHandler<ProcessSamImportHoldingMessageCommand, MessageType>> _samImportHoldingMessageHandlerMock = new();

    private readonly List<Action<IServiceCollection>> _overrideServices = [];
    private readonly IDictionary<string, string?> _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
    private readonly bool _useFakeAuth = useFakeAuth;

    private const string ComparisonReportsStorageBucket = "test-comparison-reports-bucket";

    private void ConfigureDistributedLockMock()
    {
        DistributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((string lockName, TimeSpan duration, CancellationToken ct) =>
                          {
                              if (_activeLocks.Contains(lockName))
                              {
                                  return null;
                              }

                              _activeLocks.Add(lockName);
                              var handleMock = new Mock<IDistributedLockHandle>();

                              // Set up DisposeAsync to release the lock
                              handleMock.Setup(h => h.DisposeAsync())
                                       .Callback(() => _activeLocks.Remove(lockName))
                                       .Returns(ValueTask.CompletedTask);

                              _lockHandles[lockName] = handleMock;
                              return handleMock.Object;
                          });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.ApplicationKey, typeof(Program).Assembly.FullName);

        SetTestEnvironmentVariables();

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            if (_configurationOverrides.Count > 0)
                configBuilder.AddInMemoryCollection(_configurationOverrides);
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveService<IHealthCheckPublisher>(services);

            ConfigureDistributedLockMock();

            ConfigureRepositories();
            ConfigureTransientServices();
            ConfigureTestMessageHandlers();

            ConfigureAwsOptions(services);
            ConfigureS3ClientFactory(services);
            ConfigureSimpleQueueService(services);
            ConfigureSimpleNotificationService(services);
            ConfigureDatabase(services);

            ConfigureMessageConsumers(services);

            services.AddHttpClient("DataBridgeApi")
                .ConfigurePrimaryHttpMessageHandler(() => DataBridgeApiClientHttpMessageHandlerMock.Object);

            if (_useFakeAuth)
            {
                ConfigureFakeAuthorization(services);
            }

            foreach (var applyOverride in _overrideServices)
            {
                applyOverride(services);
            }

            services.RemoveAll<IHostedService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (_configurationOverrides.Count > 0)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(_configurationOverrides);
            });
        }

        return base.CreateHost(builder);
    }

    public T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    public void OverrideServiceAsSingleton<T>(T implementation) where T : class
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddSingleton(implementation);
        });
    }

    public void OverrideServiceAsTransient<T, TH>()
        where T : class
        where TH : class, T
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddTransient<T, TH>();
        });
    }

    public void OverrideServiceAsTransient<T>(T instance)
        where T : class
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddTransient(_ => instance);
        });
    }

    public void OverrideServiceAsScoped<T>(T implementation) where T : class
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddScoped(_ => implementation);
        });
    }

    public void ResetMocks()
    {
        ResetInfrastructureMocks();
        ResetRepositoryMocks();
        ResetTransientServiceMocks();
        ResetTestMessageHandlerMocks();
        ResetDistributedLockMock();
    }

    private void ResetDistributedLockMock()
    {
        _activeLocks.Clear();
        _lockHandles.Clear();
        DistributedLockMock.Reset();
        ConfigureDistributedLockMock();
    }

    private static void SetTestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("AWS__ServiceURL", "http://localhost:4566");
        Environment.SetEnvironmentVariable("Mongo__DatabaseUri", "mongodb://localhost:27017");
        Environment.SetEnvironmentVariable("Mongo__DatabaseName", "test-keeper-data-api");
        Environment.SetEnvironmentVariable("StorageConfiguration__ComparisonReportsStorage__BucketName", ComparisonReportsStorageBucket);
        Environment.SetEnvironmentVariable("QueueConsumerOptions__IntakeEventQueueOptions__QueueUrl", "http://localhost:4566/000000000000/test-queue");
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__HealthcheckEnabled", "true");
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__BaseUrl", TestConstants.DataBridgeApiBaseUrl);
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__BridgeApiSubscriptionKey", "XYZ");
        Environment.SetEnvironmentVariable("ServiceBusSenderConfiguration__IntakeEventQueue__QueueUrl", "http://localhost:4566/000000000000/test-queue");
        Environment.SetEnvironmentVariable("DataBridgeCollectionFlags__CtsAgentsEnabled", "true");
        Environment.SetEnvironmentVariable("BulkScanEndpointsEnabled", "false");
        Environment.SetEnvironmentVariable("DailyScanEndpointsEnabled", "false");
        Environment.SetEnvironmentVariable("BatchCompletionNotificationConfiguration__BatchCompletionEventsTopic__TopicName", "ls_keeper_data_import_complete");
        Environment.SetEnvironmentVariable("BatchCompletionNotificationConfiguration__BatchCompletionEventsTopic__TopicArn", "http://localhost:4566/000000000000/ls_keeper_data_import_complete");
        Environment.SetEnvironmentVariable("AuthenticationConfiguration__EnableApiKey", "true");
        Environment.SetEnvironmentVariable("AuthenticationConfiguration__ApiGatewayExists", "true");
        Environment.SetEnvironmentVariable("AuthenticationConfiguration__Authority", "https://fake-authority/");
    }

    private static void ConfigureFakeAuthorization(IServiceCollection services)
    {
        services.RemoveAll<IConfigureNamedOptions<JwtBearerOptions>>();

        services.RemoveAll<IAuthenticationSchemeProvider>();

        services.AddSingleton<IAuthenticationSchemeProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AuthenticationOptions>>();
            var provider = new AuthenticationSchemeProvider(options);

            provider.RemoveScheme("Bearer");
            provider.AddScheme(new AuthenticationScheme(
                FakeJwtHandler.SchemeName,
                FakeJwtHandler.SchemeName,
                typeof(FakeJwtHandler)));

            return provider;
        });
    }

    private static void ConfigureMessageConsumers(IServiceCollection services)
    {
        services.RemoveAll<QueueListener>();
        services.RemoveAll<TestQueuePollerObserver<MessageType>>();
        services.RemoveAll<IDeadLetterQueueService>();
        services.RemoveAll<IQueuePoller>();

        services.AddScoped<IDeadLetterQueueService, DeadLetterQueueService>();
        services.AddScoped<IQueuePoller, QueuePoller>();

        services.AddScoped<TestQueuePollerObserver<MessageType>>();
        services.AddScoped<IQueuePollerObserver<MessageType>>(sp => sp.GetRequiredService<TestQueuePollerObserver<MessageType>>());
    }

    private void ConfigureRepositories()
    {
        OverrideServiceAsScoped(_sitesRepositoryMock.Object);
        OverrideServiceAsScoped(_partiesRepositoryMock.Object);

        OverrideServiceAsScoped(_silverCtsHoldingRepositoryMock.Object);
        OverrideServiceAsScoped(_silverCtsPartyRepositoryMock.Object);

        OverrideServiceAsScoped(_silverSamHoldingRepositoryMock.Object);
        OverrideServiceAsScoped(_silverSamPartyRepositoryMock.Object);
        OverrideServiceAsScoped(_silverSamHerdRepositoryMock.Object);

        OverrideServiceAsScoped(_goldSiteRepositoryMock.Object);
        OverrideServiceAsScoped(_goldPartyRepositoryMock.Object);
        OverrideServiceAsScoped(_goldSitePartyRoleRelationshipRepositoryMock.Object);

        OverrideServiceAsScoped(_roleRepositoryMock.Object);
        OverrideServiceAsScoped(_countryRepositoryMock.Object);
        
        ConfigureDefaultRepositoryBehavior();
    }

    private void ConfigureDefaultRepositoryBehavior()
    {
        _partiesRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartyDocument>());

        _partiesRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    private void ResetRepositoryMocks()
    {
        _sitesRepositoryMock.Reset();
        _partiesRepositoryMock.Reset();

        _silverCtsHoldingRepositoryMock.Reset();
        _silverCtsPartyRepositoryMock.Reset();

        _silverSamHoldingRepositoryMock.Reset();
        _silverSamPartyRepositoryMock.Reset();
        _silverSamHerdRepositoryMock.Reset();

        _goldSiteRepositoryMock.Reset();
        _goldPartyRepositoryMock.Reset();
        _goldSitePartyRoleRelationshipRepositoryMock.Reset();

        _roleRepositoryMock.Reset();
        _countryRepositoryMock.Reset();
    }

    private void ConfigureTransientServices()
    {
        OverrideServiceAsTransient(_countryIdentifierLookupServiceMock.Object);
        OverrideServiceAsTransient(_premiseActivityTypeLookupServiceMock.Object);
        OverrideServiceAsTransient(_activityCodeLookupServiceMock.Object);
        OverrideServiceAsTransient(_premiseTypeLookupServiceMock.Object);
        OverrideServiceAsTransient(_productionTypeLookupServiceMock.Object);
        OverrideServiceAsTransient(_productionUsageLookupServiceMock.Object);
        OverrideServiceAsTransient(_roleTypeLookupServiceMock.Object);
        OverrideServiceAsTransient(_speciesTypeLookupServiceMock.Object);
        OverrideServiceAsTransient(_siteIdentifierTypeLookupServiceMock.Object);
    }

    private void ResetTransientServiceMocks()
    {
        _countryIdentifierLookupServiceMock.Reset();
        _premiseActivityTypeLookupServiceMock.Reset();
        _premiseTypeLookupServiceMock.Reset();
        _productionTypeLookupServiceMock.Reset();
        _productionUsageLookupServiceMock.Reset();
        _roleTypeLookupServiceMock.Reset();
        _speciesTypeLookupServiceMock.Reset();
        _siteIdentifierTypeLookupServiceMock.Reset();
    }

    private void ConfigureTestMessageHandlers()
    {
        OverrideServiceAsScoped(_samImportHoldingMessageHandlerMock.Object);
    }

    private void ResetTestMessageHandlerMocks()
    {
        _samImportHoldingMessageHandlerMock.Reset();
    }

    private static void ConfigureAwsOptions(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var awsOptions = provider.GetRequiredService<AWSOptions>();
        awsOptions.Credentials = new BasicAWSCredentials("test", "test");
        services.Replace(new ServiceDescriptor(typeof(AWSOptions), awsOptions));
    }

    private void ResetInfrastructureMocks()
    {
        AmazonS3Mock!.Reset();
        AmazonSQSMock!.Reset();
        AmazonSNSMock!.Reset();
        MongoClientMock!.Reset();
        DataBridgeApiClientHttpMessageHandlerMock.Reset();

        DataBridgeApiClientHttpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                var dataBridgeResponse = new
                {
                    collectionName = "test-collection",
                    count = 0,
                    totalCount = 0,
                    skip = 0,
                    top = 100,
                    filter = (string?)null,
                    orderBy = (string?)null,
                    executedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    data = new object[0]
                };

                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(dataBridgeResponse);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
                };
                return response;
            });
    }

    private void ConfigureS3ClientFactory(IServiceCollection services)
    {
        AmazonS3Mock = new Mock<IAmazonS3>();

        AmazonS3Mock
            .Setup(x => x.GetBucketAclAsync(It.IsAny<GetBucketAclRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetBucketAclResponse { HttpStatusCode = HttpStatusCode.OK });

        AmazonS3Mock
            .Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response { HttpStatusCode = HttpStatusCode.OK });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IS3ClientFactory>();

        if (factory is S3ClientFactory concreteFactory)
        {
            concreteFactory.RegisterMockClient<ComparisonReportsStorageClient>(ComparisonReportsStorageBucket, AmazonS3Mock.Object);
        }
    }

    private void ConfigureSimpleQueueService(IServiceCollection services)
    {
        services.RemoveAll<IAmazonSQS>();

        AmazonSQSMock = new Mock<IAmazonSQS>();

        AmazonSQSMock
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        services.AddSingleton(AmazonSQSMock.Object);
    }

    private void ConfigureSimpleNotificationService(IServiceCollection services)
    {
        services.RemoveAll<IAmazonSimpleNotificationService>();

        AmazonSNSMock = new Mock<IAmazonSimpleNotificationService>();

        AmazonSNSMock
            .Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        AmazonSNSMock
            .Setup(x => x.ListTopicsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTopicsResponse() { HttpStatusCode = HttpStatusCode.OK });

        services.AddSingleton(AmazonSNSMock.Object);
    }

    private void ConfigureDatabase(IServiceCollection services)
    {
        var mongoDatabaseMock = new Mock<IMongoDatabase>();
        var mongoCollectionMock = new Mock<IMongoCollection<BsonDocument>>();
        var indexManagerMock = new Mock<IMongoIndexManager<BsonDocument>>();

        indexManagerMock
            .Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<BsonDocument>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        indexManagerMock
            .Setup(x => x.CreateManyAsync(It.IsAny<IClientSessionHandle>(), It.IsAny<IEnumerable<CreateIndexModel<BsonDocument>>>(),
                It.IsAny<CreateManyIndexesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        mongoCollectionMock
            .SetupGet(x => x.Indexes)
            .Returns(indexManagerMock.Object);

        indexManagerMock
            .Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmptyCursor());

        mongoDatabaseMock
            .Setup(x => x.GetCollection<BsonDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(mongoCollectionMock.Object);

        MongoClientMock = new Mock<IMongoClient>();

        MongoClientMock.Setup(x => x.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(mongoDatabaseMock.Object);

        services.Replace(new ServiceDescriptor(typeof(IMongoClient), MongoClientMock.Object));

        services.Replace(new ServiceDescriptor(typeof(IDistributedLock), DistributedLockMock.Object));
    }

    private static IAsyncCursor<BsonDocument> CreateEmptyCursor()
    {
        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();

        mockCursor.Setup(x => x.MoveNext(It.IsAny<CancellationToken>()))
                  .Returns(false);

        mockCursor.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        mockCursor.SetupGet(x => x.Current)
                  .Returns([]);

        return mockCursor.Object;
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        services.RemoveAll(typeof(T));
    }
}