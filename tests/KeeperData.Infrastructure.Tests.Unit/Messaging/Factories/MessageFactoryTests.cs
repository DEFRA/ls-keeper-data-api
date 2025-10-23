using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Factories.Implementations;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Factories;

public class MessageFactoryTests
{
    private readonly MessageFactory _factory = new();

    private const string TestTopicArn = "arn:aws:sns:eu-west-2:000000000000:test-topic";
    private const string TestQueueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue";

    [Fact]
    public void GivenTestMessage_WhenCallingCreateSnsMessage_ShouldSerializeBodyAndSetSubjectToTypeName()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSnsMessage(TestTopicArn, testMessage);

        result.TopicArn.Should().Be(TestTopicArn);
        result.Subject.Should().Be("TestMessage");

        result.Message.Should().Contain($"\"id\":\"{testMessage.Id}\"");
        result.Message.Should().Contain($"\"name\":\"{testMessage.Name}\"");
    }

    [Fact]
    public void GivenCustomSubject_WhenCallingCreateSnsMessage_ShouldUseProvidedSubject()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSnsMessage(TestTopicArn, testMessage, subject: "CustomSubject");

        result.Subject.Should().Be("CustomSubject");
    }

    [Fact]
    public void GivenNullAdditionalUserProperties_WhenCallingCreateSnsMessage_ShouldNotThrowAndIncludeEventTimeUtc()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSnsMessage(TestTopicArn, testMessage, additionalUserProperties: null);

        result.MessageAttributes.Should().ContainKey("EventTimeUtc");
        result.MessageAttributes.Count.Should().Be(3);
    }

    [Fact]
    public void GivenNoAdditionalProperties_WhenCallingCreateSnsMessage_ShouldIncludeEventTimeUtcOnly()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSnsMessage(TestTopicArn, testMessage);

        result.MessageAttributes.Should().ContainKey("EventTimeUtc");
        result.MessageAttributes.Count.Should().Be(3);
    }

    [Fact]
    public void GivenAdditionalUserProperties_WhenCallingCreateSnsMessage_ShouldIncludeAllAttributes()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var props = new Dictionary<string, string>
        {
            { "CustomPropertyA", "123" },
            { "CustomPropertyB", "456" }
        };

        var result = _factory.CreateSnsMessage(TestTopicArn, testMessage, additionalUserProperties: props);

        result.MessageAttributes.Should().ContainKey("EventTimeUtc");
        result.MessageAttributes.Should().ContainKey("CustomPropertyA");
        result.MessageAttributes["CustomPropertyA"].StringValue.Should().Be("123");
        result.MessageAttributes.Should().ContainKey("CustomPropertyB");
        result.MessageAttributes["CustomPropertyB"].StringValue.Should().Be("456");
    }

    [Fact]
    public void GivenTestMessage_WhenCallingCreateSqsMessage_ShouldSerializeBodyAndSetSubjectToTypeName()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSqsMessage(TestQueueUrl, testMessage);

        result.QueueUrl.Should().Be(TestQueueUrl);
        result.MessageAttributes.Should().ContainKey("Subject");
        result.MessageAttributes["Subject"].StringValue.Should().Be("Test");

        result.MessageBody.Should().Contain($"\"id\":\"{testMessage.Id}\"");
        result.MessageBody.Should().Contain($"\"name\":\"{testMessage.Name}\"");
    }

    [Fact]
    public void GivenCustomSubject_WhenCallingCreateSqsMessage_ShouldUseProvidedSubject()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSqsMessage(TestTopicArn, testMessage, subject: "CustomSubject");

        result.MessageAttributes.Should().ContainKey("Subject");
        result.MessageAttributes["Subject"].StringValue.Should().Be("CustomSubject");
    }

    [Fact]
    public void GivenNullAdditionalUserProperties_WhenCallingCreateSqsMessage_ShouldNotThrowAndIncludeEventTimeUtc()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSqsMessage(TestTopicArn, testMessage, additionalUserProperties: null);

        result.MessageAttributes.Should().ContainKey("EventTimeUtc");
        result.MessageAttributes.Count.Should().Be(3);
    }

    [Fact]
    public void GivenNoAdditionalProperties_WhenCallingCreateSqsMessage_ShouldIncludeEventTimeUtcOnly()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var result = _factory.CreateSqsMessage(TestTopicArn, testMessage);

        result.MessageAttributes.Should().ContainKey("EventTimeUtc");
        result.MessageAttributes.Count.Should().Be(3);
    }

    [Fact]
    public void GivenAdditionalUserProperties_WhenCallingCreateSqsMessage_ShouldIncludeAllAttributes()
    {
        var testMessage = new TestMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        var props = new Dictionary<string, string>
        {
            { "CustomPropertyA", "123" },
            { "CustomPropertyB", "456" }
        };

        var result = _factory.CreateSqsMessage(TestTopicArn, testMessage, additionalUserProperties: props);

        result.MessageAttributes.Should().ContainKey("EventTimeUtc");
        result.MessageAttributes.Should().ContainKey("CustomPropertyA");
        result.MessageAttributes["CustomPropertyA"].StringValue.Should().Be("123");
        result.MessageAttributes.Should().ContainKey("CustomPropertyB");
        result.MessageAttributes["CustomPropertyB"].StringValue.Should().Be("456");
    }
}

public class TestMessage
{
    public Guid Id { get; set; }
    public string? Name { get; set; } = string.Empty;
}