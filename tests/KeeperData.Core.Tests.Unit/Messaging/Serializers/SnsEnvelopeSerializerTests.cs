using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Core.Messaging.Contracts.Serializers;

namespace KeeperData.Core.Tests.Unit.Messaging.Serializers;

public class SnsEnvelopeSerializerTests
{
    private readonly SnsEnvelopeSerializer _sut = new();

    [Fact]
    public void Deserialize_ShouldReturnNull_WhenMessageIsNull()
    {
        var result = _sut.Deserialize(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ShouldReturnNull_WhenBodyIsEmpty()
    {
        var message = new Message
        {
            Body = ""
        };

        var result = _sut.Deserialize(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ShouldReturnEnvelope_WhenBodyIsValidJson()
    {
        var message = new Message
        {
            Body = """
            {
                "Type": "Notification",
                "MessageId": "00000000-0000-0000-0000-000000000001",
                "Message": "{\"id\":\"00000000-0000-0000-0000-000000000001\", \"message\":\"Test message 1\"}",
                "MessageAttributes": {
                    "Subject": { "Type": "String", "Value": "PlaceholderMessage" },
                    "CorrelationId": { "Type": "String", "Value": "00000000-0000-0000-0000-000000000002" }
                }
            }
            """
        };

        var result = _sut.Deserialize(message);

        result.Should().NotBeNull();
        result!.Type.Should().Be("Notification");
        result.MessageId.Should().Be("00000000-0000-0000-0000-000000000001");
        result.Message.Should().Contain("Test message 1");
        result.MessageAttributes.Should().ContainKey("Subject");
        result.MessageAttributes!["Subject"].Value.Should().Be("PlaceholderMessage");
    }

    [Fact]
    public void Deserialize_ShouldReturnNull_WhenBodyIsInvalidJson()
    {
        var message = new Message
        {
            Body = "badly-formed-json"
        };

        var result = _sut.Deserialize(message);

        result.Should().BeNull();
    }    
}
