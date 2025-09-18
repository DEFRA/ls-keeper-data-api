using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace KeeperData.Api.Tests.Integration;

public class IntegrationTestFixture : IDisposable
{
    public HttpClient HttpClient { get; }

    private readonly HttpClientHandler _httpClientHandler;

    private readonly AmazonSimpleNotificationServiceClient _amazonSimpleNotificationServiceClient;

    public IntegrationTestFixture()
    {
        // HttpClientHandler
        _httpClientHandler = new HttpClientHandler
        {
            AllowAutoRedirect = false
        };

        // HttpClient
        HttpClient = new HttpClient(_httpClientHandler)
        {
            BaseAddress = new Uri("http://localhost:8080"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        // SNS
        var amazonSimpleNotificationServiceConfig = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = "http://sqs.eu-west-2.localhost.localstack.cloud:4566/",
            AuthenticationRegion = "eu-west-2",
            UseHttp = true
        };
        _amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(amazonSimpleNotificationServiceConfig);
    }

    internal async Task<PublishResponse> PublishToTopicAsync(PublishRequest publishRequest, CancellationToken cancellationToken)
    {
        return await _amazonSimpleNotificationServiceClient.PublishAsync(publishRequest, cancellationToken);
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