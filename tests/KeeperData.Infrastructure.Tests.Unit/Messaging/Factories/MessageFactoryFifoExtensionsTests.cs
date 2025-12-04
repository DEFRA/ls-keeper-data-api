using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Infrastructure.Messaging.Factories.Implementations;
using KeeperData.Infrastructure.Messaging.Factories.Extensions;
using KeeperData.Infrastructure;
using System.Text.Json;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Factories;

public class MessageFactoryFifoExtensionsTests
{
    private readonly MessageFactory _messageFactory = new();

    [Theory]
    [InlineData("SamImportHoldingMessage")]
    [InlineData("SamUpdateHoldingMessage")]
    [InlineData("CtsImportHoldingMessage")]
    [InlineData("CtsUpdateHoldingMessage")]
    public void CreateFifoSqsMessage_WhenCalledWithCphBasedMessage_SetsCorrectGroupId(string messageType)
    {
        // Arrange
        var identifier = messageType.StartsWith("Cts") ? "GB-12345678" : "12345678";
        var message = CreateMessageWithIdentifier(messageType, identifier);
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message);

        // Assert
        var expectedCph = messageType.StartsWith("Cts") ? "12345678" : identifier;
        result.MessageGroupId.Should().Be($"CPH_{expectedCph}");
    }

    [Theory]
    [InlineData("SamImportHolderMessage")]
    [InlineData("CtsUpdateAgentMessage")]
    [InlineData("CtsUpdateKeeperMessage")]
    public void CreateFifoSqsMessage_WhenCalledWithPartyBasedMessage_SetsCorrectGroupId(string messageType)
    {
        // Arrange
        var identifier = "ABC12345678";
        var message = CreateMessageWithIdentifier(messageType, identifier);
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message);

        // Assert
        result.MessageGroupId.Should().Be($"PARTY_{identifier}");
    }

    [Theory]
    [InlineData("SamBulkScanMessage", "SYSTEM_SAM_BULK_SCAN")]
    [InlineData("CtsBulkScanMessage", "SYSTEM_CTS_BULK_SCAN")]
    [InlineData("SamDailyScanMessage", "SYSTEM_SAM_DAILY_SCAN")]
    [InlineData("CtsDailyScanMessage", "SYSTEM_CTS_DAILY_SCAN")]
    public void CreateFifoSqsMessage_WhenCalledWithSystemBasedMessage_SetsCorrectGroupId(string messageType, string expectedGroupId)
    {
        // Arrange
        var message = CreateMessageWithIdentifier(messageType, "");
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message);

        // Assert
        result.MessageGroupId.Should().Be(expectedGroupId);
    }

    [Fact]
    public void CreateFifoSqsMessage_WhenCalledWithLidBasedMessage_NormalizesCphInGroupId()
    {
        // Arrange
        var lid = "GB-12345678";
        var expectedCph = "12345678";
        var message = new CtsImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = lid
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message);

        // Assert
        result.MessageGroupId.Should().Be($"CPH_{expectedCph}");
    }

    [Fact]
    public void CreateFifoSqsMessage_WhenCalled_SetsMessageDeduplicationIdBasedOnContent()
    {
        // Arrange
        var message = new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = "12345678"
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result1 = _messageFactory.CreateFifoSqsMessage(queueUrl, message);
        var result2 = _messageFactory.CreateFifoSqsMessage(queueUrl, message);

        // Assert
        result1.MessageDeduplicationId.Should().NotBeNullOrEmpty();
        result2.MessageDeduplicationId.Should().NotBeNullOrEmpty();
        result1.MessageDeduplicationId.Should().Be(result2.MessageDeduplicationId,
            "same message content should generate same deduplication ID");
    }

    [Fact]
    public void CreateFifoSqsMessage_WhenCalledWithDifferentContent_SetsDifferentDeduplicationIds()
    {
        // Arrange
        var message1 = new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = "12345678"
        };
        var message2 = new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = "87654321"
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result1 = _messageFactory.CreateFifoSqsMessage(queueUrl, message1);
        var result2 = _messageFactory.CreateFifoSqsMessage(queueUrl, message2);

        // Assert
        result1.MessageDeduplicationId.Should().NotBe(result2.MessageDeduplicationId,
            "different message content should generate different deduplication IDs");
    }

    [Fact]
    public void CreateFifoSqsMessage_WhenCalled_PreservesExistingMessageAttributes()
    {
        // Arrange
        var message = new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = "12345678"
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";
        var additionalProperties = new Dictionary<string, string>
        {
            ["CustomProperty"] = "CustomValue"
        };

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message, additionalUserProperties: additionalProperties);

        // Assert
        result.MessageAttributes.Should().ContainKey("CustomProperty");
        result.MessageAttributes["CustomProperty"].StringValue.Should().Be("CustomValue");
        result.MessageAttributes.Should().ContainKey("Subject");
        result.MessageAttributes.Should().ContainKey("CorrelationId");
        result.MessageAttributes.Should().ContainKey("EventTimeUtc");
    }

    [Fact]
    public void CreateFifoSqsMessage_WhenCalled_SetsCorrectQueueUrlAndMessageBody()
    {
        // Arrange
        var message = new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = "12345678"
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message);

        // Assert
        result.QueueUrl.Should().Be(queueUrl);
        result.MessageBody.Should().NotBeNullOrEmpty();

        var deserializedMessage = JsonSerializer.Deserialize<SamImportHoldingMessage>(result.MessageBody, JsonDefaults.DefaultOptionsWithStringEnumConversion);
        deserializedMessage.Should().NotBeNull();
        deserializedMessage!.Identifier.Should().Be(message.Identifier);
        deserializedMessage.Id.Should().Be(message.Id);
    }

    [Theory]
    [InlineData("Custom Subject")]
    [InlineData(null)]
    public void CreateFifoSqsMessage_WhenCalledWithCustomSubject_SetsCorrectSubjectAttribute(string? customSubject)
    {
        // Arrange
        var message = new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = "12345678"
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message, customSubject);

        // Assert
        var expectedSubject = customSubject ?? "SamImportHolding";
        result.MessageAttributes["Subject"].StringValue.Should().Be(expectedSubject);
    }

    [Fact]
    public void CreateFifoSqsMessage_WhenMessageGroupIdContainsInvalidCharacters_NormalizesGroupId()
    {
        // Arrange
        var message = new CtsUpdateAgentMessage
        {
            Id = Guid.NewGuid(),
            Identifier = "Agent@123#456"
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act
        var result = _messageFactory.CreateFifoSqsMessage(queueUrl, message);

        // Assert
        result.MessageGroupId.Should().Be("PARTY_Agent_123_456");
        result.MessageGroupId.Should().MatchRegex("^[a-zA-Z0-9_-]+$",
            "MessageGroupId should only contain valid SQS FIFO characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateFifoSqsMessage_WhenIdentifierIsNullOrEmpty_ThrowsArgumentException(string? invalidIdentifier)
    {
        // Arrange
        var message = new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = invalidIdentifier
        };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";

        // Act & Assert
        var act = () => _messageFactory.CreateFifoSqsMessage(queueUrl, message);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Identifier*");
    }

    private static MessageType CreateMessageWithIdentifier(string messageType, string identifier)
    {
        return messageType switch
        {
            "SamImportHoldingMessage" => new SamImportHoldingMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "SamUpdateHoldingMessage" => new SamUpdateHoldingMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "CtsImportHoldingMessage" => new CtsImportHoldingMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "CtsUpdateHoldingMessage" => new CtsUpdateHoldingMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "SamImportHolderMessage" => new SamImportHolderMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "CtsUpdateAgentMessage" => new CtsUpdateAgentMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "CtsUpdateKeeperMessage" => new CtsUpdateKeeperMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "SamBulkScanMessage" => new SamBulkScanMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "CtsBulkScanMessage" => new CtsBulkScanMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "SamDailyScanMessage" => new SamDailyScanMessage { Id = Guid.NewGuid(), Identifier = identifier },
            "CtsDailyScanMessage" => new CtsDailyScanMessage { Id = Guid.NewGuid(), Identifier = identifier },
            _ => throw new ArgumentException($"Unknown message type: {messageType}")
        };
    }
}