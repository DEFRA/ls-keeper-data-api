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
    private readonly Mock<IQueueService> _dlqServiceMock = new();

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
            TestConstants.AdminDlqPeekEndpoint,
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

        var response = await httpClient.GetAsync($"{TestConstants.AdminDlqPeekEndpoint}?maxMessages=5");

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

        var response = await httpClient.GetAsync(TestConstants.AdminDlqPeekEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Default value is 10 (from service method signature)
        _dlqServiceMock.Verify(x => x.PeekDeadLetterMessagesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
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

        var response = await httpClient.GetAsync($"{TestConstants.AdminDlqPeekEndpoint}?maxMessages=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Value is passed through, service will clamp to 10
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

        // Default value is 10 (from service method signature)
        _dlqServiceMock.Verify(x => x.RedriveDeadLetterMessagesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenRedriveDlqWithExcessiveMaxMessages_ShouldPassValueToService()
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

        var response = await httpClient.PostAsync($"{TestConstants.AdminDlqRedriveEndpoint}?maxMessages=100", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Value is passed through, service will clamp to 10
        _dlqServiceMock.Verify(x => x.RedriveDeadLetterMessagesAsync(100, It.IsAny<CancellationToken>()), Times.Once);
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

        var response = await httpClient.GetAsync(TestConstants.AdminDlqPeekEndpoint);

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

    [Fact]
    public async Task GivenAdminEndpointsDisabled_WhenGetMainQueueCountRequested_ShouldReturnNotFound()
    {
        await ExecuteAdminEndpointTest(
            TestConstants.AdminMainQueueCountEndpoint,
            _dlqServiceMock.Object,
            HttpMethod.Get,
            adminEndpointsEnabled: false,
            expectedStatusCode: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GivenAdminEndpointsEnabled_WhenGetMainQueueCountRequested_ShouldSucceed()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:QueueUrl"] = "http://localhost:4566/queue/test-main-queue",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        _dlqServiceMock.Setup(x => x.GetQueueStatsAsync("http://localhost:4566/queue/test-main-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueStats
            {
                QueueUrl = "http://localhost:4566/queue/test-main-queue",
                ApproximateMessageCount = 15,
                ApproximateMessagesNotVisible = 3,
                ApproximateMessagesDelayed = 1,
                CheckedAt = DateTime.UtcNow
            });

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync(TestConstants.AdminMainQueueCountEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<QueueStats>();
        stats.Should().NotBeNull();
        stats!.QueueUrl.Should().Be("http://localhost:4566/queue/test-main-queue");
        stats.ApproximateMessageCount.Should().Be(15);
        stats.ApproximateMessagesNotVisible.Should().Be(3);
        stats.ApproximateMessagesDelayed.Should().Be(1);
    }

    [Fact]
    public async Task GivenNoMainQueueConfigured_WhenGetMainQueueCountRequested_ShouldReturnBadRequest()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:QueueUrl"] = string.Empty,
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync(TestConstants.AdminMainQueueCountEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GivenServiceThrowsException_WhenGetMainQueueCountRequested_ShouldReturnServiceUnavailable()
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = "true",
            ["QueueConsumerOptions:IntakeEventQueueOptions:QueueUrl"] = "http://localhost:4566/queue/test-main-queue",
            ["QueueConsumerOptions:IntakeEventQueueOptions:DeadLetterQueueUrl"] = "http://localhost:4566/queue/test-dlq"
        };

        _dlqServiceMock.Setup(x => x.GetQueueStatsAsync("http://localhost:4566/queue/test-main-queue", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SQS service unavailable"));

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(_dlqServiceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.GetAsync(TestConstants.AdminMainQueueCountEndpoint);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private static async Task ExecuteAdminEndpointTest(
        string endpoint,
        IQueueService dlqService,
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