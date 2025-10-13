using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Messaging.Contracts.Serializers;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = []
)]
[JsonSerializable(typeof(SamHoldingInsertedMessage))]
[JsonSerializable(typeof(SamHoldingUpdatedMessage))]
[JsonSerializable(typeof(SamHolderUpdatedMessage))]
[JsonSerializable(typeof(SamPartyUpdatedMessage))]
[JsonSerializable(typeof(SamHoldingDeletedMessage))]
[JsonSerializable(typeof(SamHolderDeletedMessage))]
[JsonSerializable(typeof(SamPartyDeletedMessage))]
[JsonSerializable(typeof(CtsHoldingInsertedMessage))]
[JsonSerializable(typeof(CtsHoldingUpdatedMessage))]
[JsonSerializable(typeof(CtsAgentUpdatedMessage))]
[JsonSerializable(typeof(CtsKeeperUpdatedMessage))]
[JsonSerializable(typeof(CtsHoldingDeletedMessage))]
[JsonSerializable(typeof(CtsAgentDeletedMessage))]
[JsonSerializable(typeof(CtsKeeperDeletedMessage))]
public partial class MessageIdentifierSerializerContext : JsonSerializerContext
{
}