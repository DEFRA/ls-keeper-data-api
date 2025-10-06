using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Messaging.Contracts.V1.Serializers;

[ExcludeFromCodeCoverage]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = []
)]
[JsonSerializable(typeof(CtsCphHoldingImportedMessage))]
public partial class CtsCphHoldingImportedMessageSerializerContext : JsonSerializerContext
{
}
