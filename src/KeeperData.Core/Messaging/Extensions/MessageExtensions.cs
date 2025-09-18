using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Core.Messaging.Extensions;

public static class MessageExtensions
{
    private const string MESSAGE_SUFFIX = "Message";

    public static T? GetMessageAttributeValue<T>(this Message message, string key)
    {
        if (message?.MessageAttributes == null || !message.MessageAttributes.TryGetValue(key, out var attribute))
            return default;

        return attribute.DataType switch
        {
            "String" when typeof(T) == typeof(string) => (T)(object)attribute.StringValue!,
            "Number" when typeof(T) == typeof(int) && int.TryParse(attribute.StringValue, out var intVal) => (T)(object)intVal,
            "Number" when typeof(T) == typeof(double) && double.TryParse(attribute.StringValue, out var doubleVal) => (T)(object)doubleVal,
            _ => default
        };
    }

    public static T? GetMessageAttributeValue<T>(this SnsEnvelope snsEnvelope, string key)
    {
        if (snsEnvelope?.MessageAttributes == null || !snsEnvelope.MessageAttributes.TryGetValue(key, out var attribute))
            return default;

        return attribute.Type switch
        {
            "String" when typeof(T) == typeof(string) => (T)(object)attribute.Value!,
            "Number" when typeof(T) == typeof(int) && int.TryParse(attribute.Value, out var intVal) => (T)(object)intVal,
            "Number" when typeof(T) == typeof(double) && double.TryParse(attribute.Value, out var doubleVal) => (T)(object)doubleVal,
            _ => default
        };
    }

    public static string ReplaceSuffix(this string? messageName)
    {
        if (messageName?.EndsWith(MESSAGE_SUFFIX) ?? false)
        {
            messageName = messageName[..messageName.LastIndexOf(MESSAGE_SUFFIX)];
        }
        return messageName ?? string.Empty;
    }
}