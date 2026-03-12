using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Api.Tests.Integration.Consumers;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class DeadLetterQueueTests : IAsyncLifetime
{
    private readonly LocalStackFixture _localStackFixture;
    private readonly string? _localStackUrl = null;
    private const string MainQueueName = "keeper_main_queue";
    private const string DlqName = "keeper_main_queue-deadletter";
    private const string TopicName = "keeper-topic";

    private string _mainQueueUrl = "";
    private string _dlqUrl = "";
    private string _topicArn = "";

    private readonly AmazonSQSClient _sqsClient;
    private readonly AmazonSimpleNotificationServiceClient _snsClient;
    private readonly ILogger<DeadLetterQueueService> _logger;

    public DeadLetterQueueTests(LocalStackFixture localStackFixture)
    {
        _localStackFixture = localStackFixture;
        _localStackUrl = _localStackFixture.SqsEndpoint;
        var creds = new Amazon.Runtime.BasicAWSCredentials("test", "test");

        _sqsClient = new AmazonSQSClient(creds, new AmazonSQSConfig
        {
            ServiceURL = _localStackUrl
        });

        _snsClient = new AmazonSimpleNotificationServiceClient(creds, new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = _localStackUrl
        });

        _logger = new LoggerFactory().CreateLogger<DeadLetterQueueService>();
    }

    public async Task InitializeAsync()
    {
        // Create DLQ
        var dlqResp = await _sqsClient.CreateQueueAsync(DlqName);
        _dlqUrl = dlqResp.QueueUrl;

        var dlqArn = (await _sqsClient.GetQueueAttributesAsync(_dlqUrl, ["QueueArn"]))
            .Attributes["QueueArn"];

        // Create main queue with redrive policy
        var mainResp = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = MainQueueName,
            Attributes = new Dictionary<string, string>
            {
                {
                    "RedrivePolicy",
                    JsonSerializer.Serialize(new { deadLetterTargetArn = dlqArn, maxReceiveCount = "3" })
                }
            }
        });

        _mainQueueUrl = mainResp.QueueUrl;

        var mainArn = (await _sqsClient.GetQueueAttributesAsync(_mainQueueUrl, ["QueueArn"]))
            .Attributes["QueueArn"];

        var topic = await _snsClient.CreateTopicAsync(TopicName);
        _topicArn = topic.TopicArn;

        var policy = $@"{{
            ""Version"": ""2012-10-17"",
            ""Statement"": [{{
                ""Effect"": ""Allow"",
                ""Principal"": ""*"",
                ""Action"": ""sqs:SendMessage"",
                ""Resource"": ""{mainArn}"",
                ""Condition"": {{
                    ""ArnEquals"": {{ ""aws:SourceArn"": ""{_topicArn}"" }}
                }}
            }}]
        }}";

        await _sqsClient.SetQueueAttributesAsync(_mainQueueUrl, new Dictionary<string, string>
        {
            { "Policy", policy }
        });

        await _snsClient.SubscribeAsync(_topicArn, "sqs", mainArn);

        await Task.Delay(1500);
        await PurgeAsync();
    }

    private async Task PurgeAsync()
    {
        try { await _sqsClient.PurgeQueueAsync(_mainQueueUrl); } catch { }
        try { await _sqsClient.PurgeQueueAsync(_dlqUrl); } catch { }
        await Task.Delay(1000);
    }

    public async Task DisposeAsync()
    {
        try { await _sqsClient.DeleteQueueAsync(_mainQueueUrl); } catch { }
        try { await _sqsClient.DeleteQueueAsync(_dlqUrl); } catch { }
        try { await _snsClient.DeleteTopicAsync(_topicArn); } catch { }
    }

    private DeadLetterQueueService CreateService()
    {
        var options = Options.Create(new IntakeEventQueueOptions
        {
            QueueUrl = _mainQueueUrl,
            DeadLetterQueueUrl = _dlqUrl
        });

        return new DeadLetterQueueService(_sqsClient, options, _logger);
    }

    private async Task<string> SendTestMessageAsync(string queueUrl, string messageBody, Dictionary<string, MessageAttributeValue>? attributes = null)
    {
        var sendResponse = await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody,
            MessageAttributes = attributes ?? new Dictionary<string, MessageAttributeValue>()
        });

        return sendResponse.MessageId;
    }

    private async Task<int> GetMessageCountAsync(string queueUrl)
    {
        var response = await _sqsClient.GetQueueAttributesAsync(queueUrl, new List<string> { "ApproximateNumberOfMessages" });
        return response.ApproximateNumberOfMessages;
    }

    [Fact]
    public async Task UnsuccessfulMessage_ShouldMoveMessageToDeadLetterQueue()
    {
        // send a message
        var msg = new { HoldingId = "test-123", Source = "Import" };

        await _snsClient.PublishAsync(_topicArn, JsonSerializer.Serialize(msg));
        await Task.Delay(500);

        // simulate consumer failure by receiving and letting visibility timeout expire
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var resp = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _mainQueueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 1,
                VisibilityTimeout = 1 // Short timeout so it expires quickly
            });

            resp.Messages.Should().HaveCount(1, $"message should be available on attempt {attempt}");

            // let the visibility timeout expire naturally to increment receive count
            await Task.Delay(1500);
        }

        // after 3 failed receives, message should be in DLQ
        await Task.Delay(500);

        await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _mainQueueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 1
        });

        // assert message is in DLQ
        var dlqCheck = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _dlqUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 1
        });

        dlqCheck.Messages.Should().HaveCount(1, "message should be in DLQ");

        // Verify the message content
        var dlqMessage = dlqCheck.Messages[0];
        var snsEnvelope = JsonSerializer.Deserialize<JsonElement>(dlqMessage.Body);
        var innerMessage = snsEnvelope.GetProperty("Message").GetString();
        var originalMsg = JsonSerializer.Deserialize<JsonElement>(innerMessage!);

        originalMsg.GetProperty("HoldingId").GetString().Should().Be("test-123");
        originalMsg.GetProperty("Source").GetString().Should().Be("Import");
    }

    [Fact]
    public async Task SuccessfullyProcessedMessage_ShouldNotMoveToDeadLetterQueue()
    {
        await _snsClient.PublishAsync(_topicArn, "{\"Id\":\"success-test\"}");
        await Task.Delay(1500);

        var receiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _mainQueueUrl,
            WaitTimeSeconds = 2
        });

        receiveResponse.Messages.Should().HaveCount(1);
        await _sqsClient.DeleteMessageAsync(_mainQueueUrl, receiveResponse.Messages[0].ReceiptHandle);

        await Task.Delay(3000);

        var dlqAttr = await _sqsClient.GetQueueAttributesAsync(
            _dlqUrl,
            ["ApproximateNumberOfMessages"]);

        dlqAttr.Attributes["ApproximateNumberOfMessages"].Should().Be("0");
    }

    [Fact]
    public async Task DeadLetterQueueAttributes_ShouldBeConfiguredCorrectly()
    {
        var mainAttrs = await _sqsClient.GetQueueAttributesAsync(
            _mainQueueUrl,
            ["RedrivePolicy"]);

        var redrive = JsonSerializer.Deserialize<RedrivePolicy>(mainAttrs.Attributes["RedrivePolicy"]);

        redrive.Should().NotBeNull();
        redrive!.maxReceiveCount.Should().Be("3");
        redrive.deadLetterTargetArn.Should().Contain(DlqName);
    }

    [Fact]
    public async Task GetQueueStatsAsync_WithRealQueue_ReturnsAccurateStats()
    {
        // Arrange
        var service = CreateService();

        await SendTestMessageAsync(_dlqUrl, "{\"test\": \"message1\"}");
        await SendTestMessageAsync(_dlqUrl, "{\"test\": \"message2\"}");
        await SendTestMessageAsync(_dlqUrl, "{\"test\": \"message3\"}");

        await Task.Delay(500);

        // Act
        var result = await service.GetQueueStatsAsync(_dlqUrl);

        // Assert
        result.Should().NotBeNull();
        result.QueueUrl.Should().Be(_dlqUrl);
        result.ApproximateMessageCount.Should().BeGreaterOrEqualTo(3);
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        await PurgeAsync();
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WithRealMessages_ReturnsMessagesWithMetadata()
    {
        // Arrange
        var service = CreateService();

        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CorrelationId"] = new MessageAttributeValue { StringValue = "test-correlation-123", DataType = "String" },
            ["Subject"] = new MessageAttributeValue { StringValue = "TestEvent", DataType = "String" },
            ["DLQ_OriginalMessageId"] = new MessageAttributeValue { StringValue = "original-msg-456", DataType = "String" },
            ["DLQ_FailureReason"] = new MessageAttributeValue { StringValue = "NonRetryableException", DataType = "String" },
            ["DLQ_FailureMessage"] = new MessageAttributeValue { StringValue = "Test failure message", DataType = "String" },
            ["DLQ_FailureTimestamp"] = new MessageAttributeValue { StringValue = DateTime.UtcNow.ToString("O"), DataType = "String" },
            ["DLQ_ReceiveCount"] = new MessageAttributeValue { StringValue = "2", DataType = "Number" }
        };

        await SendTestMessageAsync(_dlqUrl, "{\"test\": \"data\"}", attributes);
        await Task.Delay(500);

        // Act
        var result = await service.PeekDeadLetterMessagesAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().HaveCountGreaterOrEqualTo(1);

        var message = result.Messages.First();
        message.CorrelationId.Should().Be("test-correlation-123");
        message.MessageType.Should().Be("TestEvent");
        message.OriginalMessageId.Should().Be("original-msg-456");
        message.FailureReason.Should().Be("NonRetryableException");
        message.FailureMessage.Should().Be("Test failure message");
        message.ReceiveCount.Should().Be("2");
        message.Body.Should().Contain("test");

        await PurgeAsync();
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_WithRealMessages_MovesMessagesToMainQueue()
    {
        // Arrange
        var service = CreateService();

        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CorrelationId"] = new MessageAttributeValue { StringValue = "redrive-test-1", DataType = "String" },
            ["DLQ_FailureReason"] = new MessageAttributeValue { StringValue = "TestException", DataType = "String" },
            ["CustomAttribute"] = new MessageAttributeValue { StringValue = "KeepMe", DataType = "String" }
        };

        await SendTestMessageAsync(_dlqUrl, "{\"order\": \"12345\"}", attributes);
        await SendTestMessageAsync(_dlqUrl, "{\"order\": \"67890\"}");
        await Task.Delay(500);

        var dlqCountBefore = await GetMessageCountAsync(_dlqUrl);

        // Act
        var result = await service.RedriveDeadLetterMessagesAsync(2);
        await Task.Delay(1000);

        // Assert
        result.Should().NotBeNull();
        result.MessagesRedriven.Should().BeGreaterOrEqualTo(1);
        result.MessagesFailed.Should().Be(0);
        result.MessagesDuplicated.Should().Be(0);

        var mainQueueCount = await GetMessageCountAsync(_mainQueueUrl);
        mainQueueCount.Should().BeGreaterOrEqualTo(1);

        // Verify DLQ attributes were removed
        var receiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _mainQueueUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = new List<string> { "All" }
        });

        if (receiveResponse.Messages.Count > 0)
        {
            var message = receiveResponse.Messages[0];
            message.MessageAttributes.Should().NotContainKey("DLQ_FailureReason");
            message.MessageAttributes.Should().ContainKey("CustomAttribute");
        }

        await PurgeAsync();
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_WithEmptyQueue_ReturnsZeroCount()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RedriveDeadLetterMessagesAsync(10);

        // Assert
        result.Should().NotBeNull();
        result.MessagesRedriven.Should().Be(0);
        result.MessagesFailed.Should().Be(0);
        result.MessagesDuplicated.Should().Be(0);
        result.CorrelationIds.Should().BeEmpty();
    }

    [Fact]
    public async Task MoveToDeadLetterQueueAsync_WithRealMessage_MovesMessageAndAddsMetadata()
    {
        // Arrange
        var service = CreateService();

        var originalAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CorrelationId"] = new MessageAttributeValue { StringValue = "move-test-1", DataType = "String" },
            ["CustomField"] = new MessageAttributeValue { StringValue = "Important", DataType = "String" }
        };

        await SendTestMessageAsync(_mainQueueUrl, "{\"data\": \"test\"}", originalAttributes);
        await Task.Delay(500);

        var receiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _mainQueueUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = new List<string> { "All" }
        });

        receiveResponse.Messages.Should().HaveCount(1);
        var message = receiveResponse.Messages[0];

        // Act
        var result = await service.MoveToDeadLetterQueueAsync(
            message,
            _mainQueueUrl,
            new NonRetryableException("Test failure"),
            CancellationToken.None);

        await Task.Delay(1000);

        // Assert
        result.Should().BeTrue();

        var mainQueueCount = await GetMessageCountAsync(_mainQueueUrl);
        mainQueueCount.Should().Be(0);

        var dlqCount = await GetMessageCountAsync(_dlqUrl);
        dlqCount.Should().BeGreaterOrEqualTo(1);

        // Verify DLQ message has metadata
        var dlqReceiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _dlqUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = new List<string> { "All" }
        });

        dlqReceiveResponse.Messages.Should().HaveCountGreaterOrEqualTo(1);
        var dlqMessage = dlqReceiveResponse.Messages[0];

        dlqMessage.MessageAttributes.Should().ContainKey("DLQ_FailureReason");
        dlqMessage.MessageAttributes["DLQ_FailureReason"].StringValue.Should().Be("NonRetryableException");

        dlqMessage.MessageAttributes.Should().ContainKey("DLQ_FailureMessage");
        dlqMessage.MessageAttributes["DLQ_FailureMessage"].StringValue.Should().Be("Test failure");

        dlqMessage.MessageAttributes.Should().ContainKey("DLQ_OriginalMessageId");
        dlqMessage.MessageAttributes["DLQ_OriginalMessageId"].StringValue.Should().Be(message.MessageId);

        dlqMessage.MessageAttributes.Should().ContainKey("CorrelationId");
        dlqMessage.MessageAttributes["CorrelationId"].StringValue.Should().Be("move-test-1");
        dlqMessage.MessageAttributes.Should().ContainKey("CustomField");

        await PurgeAsync();
    }

    [Fact]
    public async Task PurgeDeadLetterQueueAsync_WithRealMessages_PurgesAllMessages()
    {
        // Arrange
        var service = CreateService();

        await SendTestMessageAsync(_dlqUrl, "{\"test\": \"message1\"}");
        await SendTestMessageAsync(_dlqUrl, "{\"test\": \"message2\"}");
        await SendTestMessageAsync(_dlqUrl, "{\"test\": \"message3\"}");
        await Task.Delay(500);

        var countBefore = await GetMessageCountAsync(_dlqUrl);
        countBefore.Should().BeGreaterOrEqualTo(3);

        // Act
        var result = await service.PurgeDeadLetterQueueAsync();
        await Task.Delay(1000);

        // Assert
        result.Should().NotBeNull();
        result.Purged.Should().BeTrue();
        result.ApproximateMessagesPurged.Should().BeGreaterOrEqualTo(3);

        var countAfter = await GetMessageCountAsync(_dlqUrl);
        countAfter.Should().Be(0);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_RemovesDlqAttributesFromRedrivenMessages()
    {
        // Arrange
        var service = CreateService();

        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CorrelationId"] = new MessageAttributeValue { StringValue = "attr-test-1", DataType = "String" },
            ["DLQ_FailureReason"] = new MessageAttributeValue { StringValue = "ShouldBeRemoved", DataType = "String" },
            ["DLQ_OriginalMessageId"] = new MessageAttributeValue { StringValue = "original-123", DataType = "String" },
            ["CustomAttribute"] = new MessageAttributeValue { StringValue = "ShouldBeKept", DataType = "String" }
        };

        await SendTestMessageAsync(_dlqUrl, "{\"cleanTest\": true}", attributes);
        await Task.Delay(500);

        // Act
        var redriveResult = await service.RedriveDeadLetterMessagesAsync(1);
        await Task.Delay(1000);

        // Assert
        redriveResult.MessagesRedriven.Should().BeGreaterOrEqualTo(1);

        var mainQueueReceive = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _mainQueueUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = new List<string> { "All" }
        });

        mainQueueReceive.Messages.Should().HaveCountGreaterOrEqualTo(1);
        var redrivenMessage = mainQueueReceive.Messages[0];

        redrivenMessage.MessageAttributes.Should().NotContainKey("DLQ_FailureReason");
        redrivenMessage.MessageAttributes.Should().NotContainKey("DLQ_OriginalMessageId");
        redrivenMessage.MessageAttributes.Should().ContainKey("CorrelationId");
        redrivenMessage.MessageAttributes.Should().ContainKey("CustomAttribute");
        redrivenMessage.MessageAttributes["CustomAttribute"].StringValue.Should().Be("ShouldBeKept");

        await PurgeAsync();
    }

    [Fact]
    public async Task EndToEnd_MoveToDeadLetterThenRedrive_MessageReturnedToMainQueue()
    {
        // Arrange
        var service = CreateService();

        var originalBody = "{\"orderId\": \"ORDER-123\", \"amount\": 99.99}";
        var originalAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CorrelationId"] = new MessageAttributeValue { StringValue = "end-to-end-test", DataType = "String" },
            ["Priority"] = new MessageAttributeValue { StringValue = "High", DataType = "String" }
        };

        await SendTestMessageAsync(_mainQueueUrl, originalBody, originalAttributes);
        await Task.Delay(500);

        // Step 1: Move to DLQ
        var receiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _mainQueueUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = new List<string> { "All" }
        });

        var originalMessage = receiveResponse.Messages[0];
        var moveResult = await service.MoveToDeadLetterQueueAsync(
            originalMessage,
            _mainQueueUrl,
            new NonRetryableException("Simulated failure"),
            CancellationToken.None);

        moveResult.Should().BeTrue();
        await Task.Delay(1000);

        var mainQueueAfterMove = await GetMessageCountAsync(_mainQueueUrl);
        mainQueueAfterMove.Should().Be(0);

        var dlqAfterMove = await GetMessageCountAsync(_dlqUrl);
        dlqAfterMove.Should().BeGreaterOrEqualTo(1);

        // Step 2: Redrive from DLQ
        var redriveResult = await service.RedriveDeadLetterMessagesAsync(1);
        await Task.Delay(1000);

        redriveResult.MessagesRedriven.Should().Be(1);
        redriveResult.MessagesFailed.Should().Be(0);
        redriveResult.CorrelationIds.Should().Contain("end-to-end-test");

        var mainQueueAfterRedrive = await GetMessageCountAsync(_mainQueueUrl);
        mainQueueAfterRedrive.Should().BeGreaterOrEqualTo(1);

        var dlqAfterRedrive = await GetMessageCountAsync(_dlqUrl);
        dlqAfterRedrive.Should().Be(0);

        // Step 3: Verify message integrity
        var finalReceive = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _mainQueueUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = new List<string> { "All" }
        });

        finalReceive.Messages.Should().HaveCount(1);
        var finalMessage = finalReceive.Messages[0];

        finalMessage.Body.Should().Be(originalBody);
        finalMessage.MessageAttributes.Should().ContainKey("CorrelationId");
        finalMessage.MessageAttributes["CorrelationId"].StringValue.Should().Be("end-to-end-test");
        finalMessage.MessageAttributes.Should().ContainKey("Priority");
        finalMessage.MessageAttributes.Should().NotContainKey("DLQ_FailureReason");
        finalMessage.MessageAttributes.Should().NotContainKey("DLQ_OriginalMessageId");

        await PurgeAsync();
    }

    private class RedrivePolicy
    {
        [JsonPropertyName("deadLetterTargetArn")]
        public string deadLetterTargetArn { get; set; } = string.Empty;

        [JsonPropertyName("maxReceiveCount")]
        public string maxReceiveCount { get; set; } = string.Empty;
    }
}