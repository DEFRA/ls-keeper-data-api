using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Api.Tests.Integration.Consumers;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class DeadLetterQueueTests : IAsyncLifetime
{
    // We use LocalStackFixture here but add our own queues and we need direct access to SQS/SNS clients for DLQ testing
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
        //clear queues before starting tests
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
        await Task.Delay(500); // give localstack time to move message to DLQ

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

    private class RedrivePolicy
    {
        [JsonPropertyName("deadLetterTargetArn")]
        public string deadLetterTargetArn { get; set; } = string.Empty;

        [JsonPropertyName("maxReceiveCount")]
        public string maxReceiveCount { get; set; } = string.Empty;
    }
}