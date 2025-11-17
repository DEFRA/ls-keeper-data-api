using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Extensions;
using System.Text.Json;

namespace KeeperData.Infrastructure.Messaging.Factories.Implementations;

public class MessageFactory : IMessageFactory
{
    private const string EventTimeUtc = "EventTimeUtc";

    public PublishRequest CreateSnsMessage<TBody>(
        string topicArn,
        TBody body,
        string? subject = null,
        Dictionary<string, string>? additionalUserProperties = null)
    {
        var messageType = typeof(TBody).Name;
        var payload = SerializeToJson(body);
        var resolvedSubject = subject ?? messageType;

        var request = new PublishRequest(topicArn, payload, resolvedSubject)
        {
            MessageAttributes = BuildSnsAttributes(resolvedSubject, additionalUserProperties)
        };

        return request;
    }

    public SendMessageRequest CreateSqsMessage<TBody>(
        string queueUrl,
        TBody body,
        string? subject = null,
        Dictionary<string, string>? additionalUserProperties = null)
    {
        var messageType = typeof(TBody).Name;
        var payload = SerializeToJson(body);
        var resolvedSubject = subject ?? messageType;

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = payload,
            MessageAttributes = BuildSqsAttributes(resolvedSubject, additionalUserProperties)
        };

        return request;
    }

    private static Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue> BuildSnsAttributes(
        string subject,
        Dictionary<string, string>? additionalUserProperties)
    {
        var attributes = new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>
        {
            [EventTimeUtc] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = DateTime.UtcNow.ToString("O")
            },
            ["Subject"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = subject.ReplaceSuffix()
            },
            ["CorrelationId"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = CorrelationIdContext.Value ?? Guid.NewGuid().ToString()
            }
        };

        foreach (var (key, value) in additionalUserProperties ?? [])
        {
            attributes[key] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = value
            };
        }

        return attributes;
    }

    private static Dictionary<string, Amazon.SQS.Model.MessageAttributeValue> BuildSqsAttributes(
        string subject,
        Dictionary<string, string>? additionalUserProperties)
    {
        var attributes = new Dictionary<string, Amazon.SQS.Model.MessageAttributeValue>
        {
            [EventTimeUtc] = new Amazon.SQS.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = DateTime.UtcNow.ToString("O")
            },
            ["Subject"] = new Amazon.SQS.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = subject.ReplaceSuffix()
            },
            ["CorrelationId"] = new Amazon.SQS.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = CorrelationIdContext.Value ?? Guid.NewGuid().ToString()
            }
        };

        foreach (var (key, value) in additionalUserProperties ?? [])
        {
            attributes[key] = new Amazon.SQS.Model.MessageAttributeValue
            {
                DataType = "String",
                StringValue = value
            };
        }

        return attributes;
    }

    private static string SerializeToJson<TBody>(TBody value)
    {
        return typeof(TBody) switch
        {
            // Add specific 'Source Generations' here for message types
            _ => JsonSerializer.Serialize(value, JsonDefaults.DefaultOptionsWithStringEnumConversion)
        };
    }
}