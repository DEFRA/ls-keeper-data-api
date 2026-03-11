using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using KeeperData.Api.Setup;
using KeeperData.Core.DeadLetter;

namespace KeeperData.Application.Tests.Unit.MessageHandlers.DeadLetter;

public class DeadLetterHandlerTests
{
    private readonly Mock<IDeadLetterQueueService> _dlqServiceMock = new();
    private readonly Mock<IOptions<IntakeEventQueueOptions>> _queueOptionsMock = new();
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private const string ValidDlqUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-dlq";

    private void SetupQueueOptions(string? dlqUrl = ValidDlqUrl)
    {
        _queueOptionsMock.Setup(o => o.Value)
            .Returns(new IntakeEventQueueOptions
            {
                DeadLetterQueueUrl = dlqUrl,
                QueueUrl = "testUrl"
            });
    }

    [Fact]
    public async Task GetDeadLetterQueueCountHandler_WhenDlqUrlIsNull_ReturnsBadRequest()
    {
        // Arrange
        SetupQueueOptions(null);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterQueueCountHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("BadRequest");
        _dlqServiceMock.Verify(s => s.GetQueueStatsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDeadLetterQueueCountHandler_WhenDlqUrlIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        SetupQueueOptions(string.Empty);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterQueueCountHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("BadRequest");
    }

    [Fact]
    public async Task GetDeadLetterQueueCountHandler_WhenDlqUrlIsWhitespace_ReturnsBadRequest()
    {
        // Arrange
        SetupQueueOptions("   ");

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterQueueCountHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("BadRequest");
    }

    [Fact]
    public async Task GetDeadLetterQueueCountHandler_WhenServiceSucceeds_ReturnsOkWithStats()
    {
        // Arrange
        SetupQueueOptions();
        var expectedStats = new QueueStats
        {
            ApproximateMessageCount = 10,
            ApproximateMessagesNotVisible = 2,
            CheckedAt = DateTime.UtcNow
        };

        _dlqServiceMock.Setup(s => s.GetQueueStatsAsync(ValidDlqUrl, _cancellationToken))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterQueueCountHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.Should().BeOfType<Ok<QueueStats>>();
        var okResult = (Ok<QueueStats>)result;
        okResult.Value.Should().Be(expectedStats);
        _dlqServiceMock.Verify(s => s.GetQueueStatsAsync(ValidDlqUrl, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetDeadLetterQueueCountHandler_WhenServiceThrowsException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupQueueOptions();
        _dlqServiceMock.Setup(s => s.GetQueueStatsAsync(ValidDlqUrl, _cancellationToken))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterQueueCountHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("JsonHttpResult");
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty!.GetValue(result).Should().Be(503);
    }

    [Fact]
    public async Task GetDeadLetterMessagesHandler_WhenDlqUrlIsNull_ReturnsBadRequest()
    {
        // Arrange
        SetupQueueOptions(null);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterMessagesHandler(
            5,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("BadRequest");
    }

    [Fact]
    public async Task GetDeadLetterMessagesHandler_WhenMaxMessagesIsNull_UsesDefaultValue()
    {
        // Arrange
        SetupQueueOptions();
        var expectedResult = new DeadLetterMessagesResult
        {
            Messages = [],
            TotalApproximateCount = 0,
            CheckedAt = DateTime.UtcNow
        };

        _dlqServiceMock.Setup(s => s.PeekDeadLetterMessagesAsync(5, _cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterMessagesHandler(
            null,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.Should().BeOfType<Ok<DeadLetterMessagesResult>>();
        _dlqServiceMock.Verify(s => s.PeekDeadLetterMessagesAsync(5, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetDeadLetterMessagesHandler_WhenMaxMessagesIsLessThanOne_ClampsToOne()
    {
        // Arrange
        SetupQueueOptions();
        var expectedResult = new DeadLetterMessagesResult();

        _dlqServiceMock.Setup(s => s.PeekDeadLetterMessagesAsync(1, _cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterMessagesHandler(
            0,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        _dlqServiceMock.Verify(s => s.PeekDeadLetterMessagesAsync(1, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetDeadLetterMessagesHandler_WhenMaxMessagesIsGreaterThanTen_ClampsToTen()
    {
        // Arrange
        SetupQueueOptions();
        var expectedResult = new DeadLetterMessagesResult();

        _dlqServiceMock.Setup(s => s.PeekDeadLetterMessagesAsync(10, _cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterMessagesHandler(
            100,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        _dlqServiceMock.Verify(s => s.PeekDeadLetterMessagesAsync(10, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetDeadLetterMessagesHandler_WhenServiceSucceeds_ReturnsOkWithMessages()
    {
        // Arrange
        SetupQueueOptions();
        var expectedResult = new DeadLetterMessagesResult
        {
            Messages = [new DeadLetterMessageDto { MessageId = "msg-123", Body = "test" }],
            TotalApproximateCount = 1,
            CheckedAt = DateTime.UtcNow
        };

        _dlqServiceMock.Setup(s => s.PeekDeadLetterMessagesAsync(5, _cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterMessagesHandler(
            5,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.Should().BeOfType<Ok<DeadLetterMessagesResult>>();
        var okResult = (Ok<DeadLetterMessagesResult>)result;
        okResult.Value.Should().Be(expectedResult);
    }

    [Fact]
    public async Task GetDeadLetterMessagesHandler_WhenServiceThrowsException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupQueueOptions();
        _dlqServiceMock.Setup(s => s.PeekDeadLetterMessagesAsync(It.IsAny<int>(), _cancellationToken))
            .ThrowsAsync(new Exception("SQS timeout"));

        // Act
        var result = await WebApplicationExtensions.GetDeadLetterMessagesHandler(
            5,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("JsonHttpResult");
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty!.GetValue(result).Should().Be(503);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesHandler_WhenDlqUrlIsNull_ReturnsBadRequest()
    {
        // Arrange
        SetupQueueOptions(null);

        // Act
        var result = await WebApplicationExtensions.RedriveDeadLetterMessagesHandler(
            10,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("BadRequest");
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesHandler_WhenMaxMessagesIsNull_UsesDefaultValue()
    {
        // Arrange
        SetupQueueOptions();
        var expectedSummary = new RedriveSummary { MessagesRedriven = 5 };

        _dlqServiceMock.Setup(s => s.RedriveDeadLetterMessagesAsync(10, _cancellationToken))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await WebApplicationExtensions.RedriveDeadLetterMessagesHandler(
            null,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        _dlqServiceMock.Verify(s => s.RedriveDeadLetterMessagesAsync(10, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesHandler_WhenMaxMessagesIsLessThanOne_ClampsToOne()
    {
        // Arrange
        SetupQueueOptions();
        var expectedSummary = new RedriveSummary();

        _dlqServiceMock.Setup(s => s.RedriveDeadLetterMessagesAsync(1, _cancellationToken))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await WebApplicationExtensions.RedriveDeadLetterMessagesHandler(
            -5,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        _dlqServiceMock.Verify(s => s.RedriveDeadLetterMessagesAsync(1, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesHandler_WhenMaxMessagesIsGreaterThan100_ClampsTo100()
    {
        // Arrange
        SetupQueueOptions();
        var expectedSummary = new RedriveSummary();

        _dlqServiceMock.Setup(s => s.RedriveDeadLetterMessagesAsync(100, _cancellationToken))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await WebApplicationExtensions.RedriveDeadLetterMessagesHandler(
            500,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        _dlqServiceMock.Verify(s => s.RedriveDeadLetterMessagesAsync(100, _cancellationToken), Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesHandler_WhenServiceSucceeds_ReturnsOkWithSummary()
    {
        // Arrange
        SetupQueueOptions();
        var expectedSummary = new RedriveSummary
        {
            MessagesRedriven = 10,
            MessagesFailed = 0,
            MessagesDuplicated = 0,
            MessagesRemainingApprox = 5,
            StartedAt = DateTime.UtcNow.AddMinutes(-1),
            CompletedAt = DateTime.UtcNow
        };

        _dlqServiceMock.Setup(s => s.RedriveDeadLetterMessagesAsync(10, _cancellationToken))
            .ReturnsAsync(expectedSummary);

        // Act
        var result = await WebApplicationExtensions.RedriveDeadLetterMessagesHandler(
            10,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.Should().BeOfType<Ok<RedriveSummary>>();
        var okResult = (Ok<RedriveSummary>)result;
        okResult.Value.Should().Be(expectedSummary);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesHandler_WhenServiceThrowsException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupQueueOptions();
        _dlqServiceMock.Setup(s => s.RedriveDeadLetterMessagesAsync(It.IsAny<int>(), _cancellationToken))
            .ThrowsAsync(new Exception("Connection refused"));

        // Act
        var result = await WebApplicationExtensions.RedriveDeadLetterMessagesHandler(
            10,
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("JsonHttpResult");
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty!.GetValue(result).Should().Be(503);
    }

    [Fact]
    public async Task PurgeDeadLetterQueueHandler_WhenDlqUrlIsNull_ReturnsBadRequest()
    {
        // Arrange
        SetupQueueOptions(null);

        // Act
        var result = await WebApplicationExtensions.PurgeDeadLetterQueueHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("BadRequest");
    }

    [Fact]
    public async Task PurgeDeadLetterQueueHandler_WhenServiceSucceeds_ReturnsOkWithResult()
    {
        // Arrange
        SetupQueueOptions();
        var expectedResult = new PurgeResult
        {
            Purged = true,
            ApproximateMessagesPurged = 50,
            PurgedAt = DateTime.UtcNow
        };

        _dlqServiceMock.Setup(s => s.PurgeDeadLetterQueueAsync(_cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await WebApplicationExtensions.PurgeDeadLetterQueueHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.Should().BeOfType<Ok<PurgeResult>>();
        var okResult = (Ok<PurgeResult>)result;
        okResult.Value.Should().Be(expectedResult);
    }

    [Fact]
    public async Task PurgeDeadLetterQueueHandler_WhenPurgeInProgress_ReturnsTooManyRequests()
    {
        // Arrange
        SetupQueueOptions();
        _dlqServiceMock.Setup(s => s.PurgeDeadLetterQueueAsync(_cancellationToken))
            .ThrowsAsync(new PurgeQueueInProgressException("Purge already in progress"));

        // Act
        var result = await WebApplicationExtensions.PurgeDeadLetterQueueHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("JsonHttpResult");
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty!.GetValue(result).Should().Be(429);
    }

    [Fact]
    public async Task PurgeDeadLetterQueueHandler_WhenServiceThrowsException_ReturnsServiceUnavailable()
    {
        // Arrange
        SetupQueueOptions();
        _dlqServiceMock.Setup(s => s.PurgeDeadLetterQueueAsync(_cancellationToken))
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act
        var result = await WebApplicationExtensions.PurgeDeadLetterQueueHandler(
            _dlqServiceMock.Object,
            _queueOptionsMock.Object,
            NullLogger<Program>.Instance,
            _cancellationToken);

        // Assert
        result.GetType().Name.Should().StartWith("JsonHttpResult");
        var statusCodeProperty = result.GetType().GetProperty("StatusCode");
        statusCodeProperty!.GetValue(result).Should().Be(503);
    }
}