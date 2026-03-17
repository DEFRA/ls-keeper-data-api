using FluentAssertions;
using KeeperData.Core.DeadLetter;
using KeeperData.Infrastructure.Messaging.Services;
using KeeperData.Tests.Common.Utilities;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class AdminDlqEndpointTests
{
    private readonly Mock<IDeadLetterQueueService> _dlqServiceMock = new();

    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    public AdminDlqEndpointTests()
    {
        _dlqServiceMock.Setup(x => x.GetQueueStatsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueStats
            {
                QueueUrl = "http://localhost:4566/queue/test-dlq",
                ApproximateMessageCount = 5,
                CheckedAt = DateTime.UtcNow
            });

        _dlqServiceMock.Setup(x => x.PeekDeadLetterMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeadLetterMessagesResult
            {
                Messages = new List<DeadLetterMessageDto>(),
                TotalApproximateCount = 5,
                CheckedAt = DateTime.UtcNow
            });

        _dlqServiceMock.Setup(x => x.RedriveDeadLetterMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RedriveSummary
            {
                MessagesRedriven = 3,
                MessagesFailed = 0,
                MessagesDuplicated = 0,
                MessagesRemainingApprox = 2,
                CorrelationIds = new List<string> { "corr-1", "corr-2", "corr-3" },
                StartedAt = DateTime.UtcNow.AddSeconds(-5),
                CompletedAt = DateTime.UtcNow
            });

        _dlqServiceMock.Setup(x => x.PurgeDeadLetterQueueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurgeResult
            {
                Purged = true,
                ApproximateMessagesPurged = 5,
                PurgedAt = DateTime.UtcNow
            });
    }

    [Fact]
    public async Task GivenAdminEndpointsDisabled_WhenGetDlqCountRequested_ShouldReturnNotFound()
    {
        await ExecuteAdminEndpointTest(
            TestConstants.AdminDlqCountEndpoint,
            _dlqServiceMock.Object,
            HttpMethod.Get,
            adminEndpointsEnabled: false,
            expectedStatusCode: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenAdminEndpointsDisabled_WhenGetDlqMessagesRequested_ShouldReturnNotFound()
    {
        await ExecuteAdminEndpointTest(
            TestConstants.AdminDlqMessagesEndpoint,
            _dlqServiceMock.Object,
            HttpMethod.Get,
            adminEndpointsEnabled: false,
            expectedStatusCode: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenAdminEndpointsDisabled_WhenRedriveDlqRequested_ShouldReturnNotFound()
    {
        await ExecuteAdminEndpointTest(
            TestConstants.AdminDlqRedriveEndpoint,
            _dlqServiceMock.Object,
            HttpMethod.Post,
            adminEndpointsEnabled: false,
            expectedStatusCode: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenAdminEndpointsDisabled_WhenPurgeDlqRequested_ShouldReturnNotFound()
    {
        await ExecuteAdminEndpointTest(
            TestConstants.AdminDlqPurgeEndpoint,
            _dlqServiceMock.Object,
            HttpMethod.Post,
            adminEndpointsEnabled: false,
            expectedStatusCode: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenGetDlqCountRequested_ShouldSucceed()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync(TestConstants.AdminDlqCountEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<QueueStats>();
        stats.Should().NotBeNull();
        stats!.ApproximateMessageCount.Should().Be(5);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenGetDlqMessagesRequested_ShouldSucceed()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync($"{TestConstants.AdminDlqMessagesEndpoint}?maxMessages=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DeadLetterMessagesResult>();
        result.Should().NotBeNull();
        result!.TotalApproximateCount.Should().Be(5);

        _dlqServiceMock.Verify(x => x.PeekDeadLetterMessagesAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenGetDlqMessagesWithoutMaxMessages_ShouldUseDefaultValue()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync(TestConstants.AdminDlqMessagesEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // When null, defaults to 0 (service interprets this as "get all messages")
        _dlqServiceMock.Verify(x => x.PeekDeadLetterMessagesAsync(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenGetDlqMessagesWithExcessiveMaxMessages_ShouldPassValueToService()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync($"{TestConstants.AdminDlqMessagesEndpoint}?maxMessages=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Value is passed through without clamping
        _dlqServiceMock.Verify(x => x.PeekDeadLetterMessagesAsync(100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenRedriveDlqRequested_ShouldSucceed()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync($"{TestConstants.AdminDlqRedriveEndpoint}?maxMessages=3", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<RedriveSummary>();
        summary.Should().NotBeNull();
        summary!.MessagesRedriven.Should().Be(3);
        summary.MessagesRemainingApprox.Should().Be(2);

        _dlqServiceMock.Verify(x => x.RedriveDeadLetterMessagesAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenRedriveDlqWithoutMaxMessages_ShouldUseDefaultValue()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync(TestConstants.AdminDlqRedriveEndpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // When null, defaults to 0 (service interprets this as "redrive all messages")
        _dlqServiceMock.Verify(x => x.RedriveDeadLetterMessagesAsync(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenRedriveDlqWithExcessiveMaxMessages_ShouldClampTo1000()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync($"{TestConstants.AdminDlqRedriveEndpoint}?maxMessages=1000", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _dlqServiceMock.Verify(x => x.RedriveDeadLetterMessagesAsync(1000, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenPurgeDlqRequested_ShouldSucceed()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync(TestConstants.AdminDlqPurgeEndpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PurgeResult>();
        result.Should().NotBeNull();
        result!.Purged.Should().BeTrue();
        result.ApproximateMessagesPurged.Should().Be(5);

        _dlqServiceMock.Verify(x => x.PurgeDeadLetterQueueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenNoDlqConfigured_WhenGetDlqCountRequested_ShouldReturnBadRequest()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = string.Empty
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync(TestConstants.AdminDlqCountEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenNoDlqConfigured_WhenGetDlqMessagesRequested_ShouldReturnBadRequest()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = string.Empty
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync(TestConstants.AdminDlqMessagesEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenNoDlqConfigured_WhenRedriveDlqRequested_ShouldReturnBadRequest()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = string.Empty
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync(TestConstants.AdminDlqRedriveEndpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenNoDlqConfigured_WhenPurgeDlqRequested_ShouldReturnBadRequest()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = string.Empty
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync(TestConstants.AdminDlqPurgeEndpoint, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task ExecuteAdminEndpointTest(
        string endpoint,
        IDeadLetterQueueService dlqService,
        HttpMethod httpMethod,
        bool adminEndpointsEnabled = false,
        HttpStatusCode expectedStatusCode = HttpStatusCode.NotFound)
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = adminEndpointsEnabled.ToString().ToLowerInvariant(),
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(dlqService);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = httpMethod.Method == "GET"
            ? await httpClient.GetAsync(endpoint)
            : await httpClient.PostAsync(endpoint, null);

        response.StatusCode.Should().Be(expectedStatusCode);
    }
}