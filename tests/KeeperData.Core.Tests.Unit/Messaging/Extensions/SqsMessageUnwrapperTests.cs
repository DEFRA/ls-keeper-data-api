using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Core.Messaging.Serializers;
using Moq;

namespace KeeperData.Core.Tests.Unit.Messaging.Extensions;

public class SqsMessageUnwrapperTests
{
    private const string Payload = "{\"id\":\"00000000-0000-0000-0000-000000000001\", \"message\":\"Test message 1\"}";
    private const string SubjectKey = "Subject";
    private const string CorrelationIdKey = "CorrelationId";

    [Fact]
    public void GivenMessageIsNull_WhenCallingUnwrap_ShouldThrowArgumentNullException()
    {
        var serializerMock = new Mock<IMessageSerializer<SnsEnvelope>>();
        Action act = () => ((Message)null!).Unwrap(serializerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenSnsNotificationEnvelope_WhenCallingUnwrap_ShouldReturnUnwrappedMessage()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        var envelope = BuildSnsEnvelope(messageId, correlationId, "PlaceholderMessage");
        var serializerMock = new Mock<IMessageSerializer<SnsEnvelope>>();
        serializerMock.Setup(x => x.Deserialize(It.IsAny<Message>())).Returns(envelope);

        var message = new Message { MessageId = Guid.NewGuid().ToString(), Body = string.Empty };
        var result = message.Unwrap(serializerMock.Object);

        VerifyUnwrappedMessage(messageId, correlationId, "Placeholder", Payload, result);
    }

    [Fact]
    public void GivenSnsEnvelopeIsNull_WhenCallingUnwrap_ShouldFallbackToRawMessage()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        var serializerMock = new Mock<IMessageSerializer<SnsEnvelope>>();
        serializerMock.Setup(x => x.Deserialize(It.IsAny<Message>())).Returns(() => null);

        var message = BuildSqsMessage(messageId, correlationId, "PlaceholderMessage");
        var result = message.Unwrap(serializerMock.Object);

        VerifyUnwrappedMessage(messageId, correlationId, "Placeholder", Payload, result);
    }

    [Fact]
    public void GivenDeserializationThrows_WhenCallingUnwrap_ShouldFallbackToRawMessage()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();

        var serializerMock = new Mock<IMessageSerializer<SnsEnvelope>>();
        serializerMock.Setup(x => x.Deserialize(It.IsAny<Message>())).Throws(new Exception("Boom"));

        var message = BuildSqsMessage(messageId, correlationId, "PlaceholderMessage");
        var result = message.Unwrap(serializerMock.Object);

        VerifyUnwrappedMessage(messageId, correlationId, "Placeholder", Payload, result);
    }

    [Fact]
    public void GivenSnsNotificationEnvelopeWithMissingAttributes_ShouldUseDefaults()
    {
        var envelope = new SnsEnvelope
        {
            Type = "Notification",
            MessageId = "sns-id",
            Message = Payload,
            MessageAttributes = new Dictionary<string, SnsMessageAttribute>
            {
                [SubjectKey] = new() { Type = "String" },
                [CorrelationIdKey] = new() { Type = "String", Value = null }
            }
        };

        var serializerMock = new Mock<IMessageSerializer<SnsEnvelope>>();
        serializerMock.Setup(x => x.Deserialize(It.IsAny<Message>())).Returns(envelope);

        var message = new Message { MessageId = "ignored", Body = string.Empty };
        var result = message.Unwrap(serializerMock.Object);

        result.Subject.Should().Be("Default");
        result.CorrelationId.Should().Be(string.Empty);
    }

    [Fact]
    public void GivenRawMessageWithMissingAttributes_ShouldUseDefaults()
    {
        var serializerMock = new Mock<IMessageSerializer<SnsEnvelope>>();
        serializerMock.Setup(x => x.Deserialize(It.IsAny<Message>())).Returns(() => null);

        var message = new Message
        {
            MessageId = "raw-id",
            Body = Payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                [SubjectKey] = new() { DataType = "String" },
                [CorrelationIdKey] = new() { DataType = "String", StringValue = null }
            }
        };

        var result = message.Unwrap(serializerMock.Object);

        result.Subject.Should().Be("Default");
        result.CorrelationId.Should().Be(string.Empty);
    }

    [Fact]
    public void GivenSnsEnvelopeWithNonNotificationType_ShouldFallbackToRawMessage()
    {
        var serializerMock = new Mock<IMessageSerializer<SnsEnvelope>>();
        serializerMock.Setup(x => x.Deserialize(It.IsAny<Message>())).Returns(new SnsEnvelope { Type = "Other" });

        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var message = BuildSqsMessage(messageId, correlationId, "PlaceholderMessage");

        var result = message.Unwrap(serializerMock.Object);

        VerifyUnwrappedMessage(messageId, correlationId, "Placeholder", Payload, result);
    }

    private static SnsEnvelope BuildSnsEnvelope(string messageId, string correlationId, string subject)
    {
        return new SnsEnvelope
        {
            Type = "Notification",
            MessageId = messageId,
            Message = Payload,
            MessageAttributes = new Dictionary<string, SnsMessageAttribute>
            {
                [SubjectKey] = new() { Type = "String", Value = subject },
                [CorrelationIdKey] = new() { Type = "String", Value = correlationId }
            }
        };
    }

    private static Message BuildSqsMessage(string messageId, string correlationId, string subject)
    {
        return new Message
        {
            MessageId = messageId,
            Body = Payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                [SubjectKey] = new() { DataType = "String", StringValue = subject },
                [CorrelationIdKey] = new() { DataType = "String", StringValue = correlationId }
            }
        };
    }

    private static void VerifyUnwrappedMessage(string expectedId, string expectedCorrelationId, string expectedSubject, string expectedPayload, UnwrappedMessage actual)
    {
        actual.MessageId.Should().Be(expectedId);
        actual.CorrelationId.Should().Be(expectedCorrelationId);
        actual.Subject.Should().Be(expectedSubject);
        actual.Payload.Should().Be(expectedPayload);
        actual.Attributes.Should().ContainKeys(SubjectKey, CorrelationIdKey);
    }
}