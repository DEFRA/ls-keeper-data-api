using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Infrastructure.Messaging.Factories;
using KeeperData.Infrastructure.Messaging.Fifo;
using KeeperData.Infrastructure;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KeeperData.Infrastructure.Messaging.Factories.Extensions;

public static class MessageFactoryFifoExtensions
{
    public static SendMessageRequest CreateFifoSqsMessage<TBody>(
        this IMessageFactory messageFactory,
        string queueUrl,
        TBody body,
        string? subject = null,
        Dictionary<string, string>? additionalUserProperties = null)
        where TBody : MessageType
    {
        // Create the base SQS message using existing method
        var request = messageFactory.CreateSqsMessage(queueUrl, body, subject, additionalUserProperties);

        // Add FIFO-specific properties
        request.MessageGroupId = MessageGroupIdExtractor.ExtractGroupId(body);
        request.MessageDeduplicationId = GenerateMessageDeduplicationId(body);

        return request;
    }

    private static string GenerateMessageDeduplicationId<TBody>(TBody body)
    {
        // Serialize the message body to JSON for content-based deduplication
        var jsonContent = JsonSerializer.Serialize(body, JsonDefaults.DefaultOptionsWithStringEnumConversion);

        // Generate SHA-256 hash of the content
        var contentBytes = Encoding.UTF8.GetBytes(jsonContent);
        var hashBytes = SHA256.HashData(contentBytes);

        // Convert to hex string and take first 128 characters (SQS limit)
        var hashHex = Convert.ToHexString(hashBytes);
        return hashHex.Length > 128 ? hashHex[..128] : hashHex;
    }
}