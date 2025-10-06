using KeeperData.Core.Messaging.Contracts.V1.Sam;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Messaging.Contracts.V1.Serializers;

[ExcludeFromCodeCoverage]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = []
)]
[JsonSerializable(typeof(SamHoldingInsertedMessage))]
public partial class SamHoldingInsertedMessageSerializerContext : JsonSerializerContext
{
}