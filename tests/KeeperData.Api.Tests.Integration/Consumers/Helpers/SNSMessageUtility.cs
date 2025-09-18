using Amazon.SimpleNotificationService.Model;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Api.Tests.Integration.Consumers.Helpers;

public class SNSMessageUtility
{
    internal static PublishRequest CreateMessage<TBody>(
        string topicArn,
        TBody body,
        string subject,
        Dictionary<string, string>? additionalUserProperties)
    {
        var messageType = typeof(TBody).Name;

        return GenerateMessage(
            topicArn,
            SerializeToJson(body),
            subject ?? messageType,
            additionalUserProperties: additionalUserProperties);
    }

    private static PublishRequest GenerateMessage(
        string topicArn,
        string body,
        string subject,
        Dictionary<string, string>? additionalUserProperties)
    {
        var message = new PublishRequest(topicArn, body, subject)
        {
            MessageAttributes = []
        };

        foreach (var (key, value) in additionalUserProperties ?? [])
        {
            message.MessageAttributes.Add(key, new MessageAttributeValue
            {
                DataType = "String",
                StringValue = value
            });
        }

        message.MessageAttributes.TryAdd("Subject", new MessageAttributeValue
        {
            DataType = "String",
            StringValue = subject.ReplaceSuffix()
        });

        return message;
    }

    private static string SerializeToJson<TBody>(TBody value)
    {
        return typeof(TBody) switch
        {
            _ => JsonSerializer.Serialize(value, JsonDefaults.DefaultOptionsWithStringEnumConversion)
        };
    }
}
