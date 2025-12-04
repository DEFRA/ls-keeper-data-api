using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Api.Tests.Integration.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace KeeperData.Api.Tests.Integration;

public class IntegrationTestFixture : IDisposable
{
    public HttpClient HttpClient { get; }

    public MongoVerifier MongoVerifier { get; }

    private readonly HttpClientHandler _httpClientHandler;

    private readonly AmazonSimpleNotificationServiceClient _amazonSimpleNotificationServiceClient;

    private readonly AmazonSQSClient _amazonSQSClient;

    private static bool s_mongoGlobalsRegistered;

    public IntegrationTestFixture()
    {
        // Register MongoDB globals
        RegisterMongoGlobals();

        // HttpClientHandler
        _httpClientHandler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        // HttpClient
        HttpClient = new HttpClient(_httpClientHandler)
        {
            BaseAddress = new Uri("http://localhost:5555"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // SNS & SQS
        var credentials = new Amazon.Runtime.BasicAWSCredentials("test", "test");
        var amazonSimpleNotificationServiceConfig = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = "http://localhost:4568",
            AuthenticationRegion = "eu-west-2",
            UseHttp = true
        };
        _amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(credentials, amazonSimpleNotificationServiceConfig);

        var amazonSQSconfig = new AmazonSQSConfig
        {
            ServiceURL = "http://localhost:4568",
            AuthenticationRegion = "eu-west-2",
            UseHttp = true
        };
        _amazonSQSClient = new AmazonSQSClient(credentials, amazonSQSconfig);

        // Mongo
        MongoVerifier = new MongoVerifier("mongodb://localhost:27019", "ls-keeper-data-api");
    }

    internal async Task<PublishResponse> PublishToTopicAsync(PublishRequest publishRequest, CancellationToken cancellationToken)
    {
        return await _amazonSimpleNotificationServiceClient.PublishAsync(publishRequest, cancellationToken);
    }

    internal async Task<SendMessageResponse> PublishToQueueAsync(SendMessageRequest sendMessageRequest, CancellationToken cancellationToken)
    {
        return await _amazonSQSClient.SendMessageAsync(sendMessageRequest, cancellationToken);
    }

    internal async Task<SendMessageResponse> PublishToFifoQueueAsync(SendMessageRequest sendMessageRequest, CancellationToken cancellationToken)
    {
        return await _amazonSQSClient.SendMessageAsync(sendMessageRequest, cancellationToken);
    }

    private static void RegisterMongoGlobals()
    {
        if (s_mongoGlobalsRegistered) return;

        var existing = BsonSerializer.LookupSerializer(typeof(Guid));
        if (existing is not GuidSerializer)
        {
            BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
        }

        ConventionRegistry.Register(
            "CamelCase",
            new ConventionPack { new CamelCaseElementNameConvention() },
            _ => true
        );

        s_mongoGlobalsRegistered = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HttpClient?.Dispose();
            _httpClientHandler?.Dispose();
        }
    }
}