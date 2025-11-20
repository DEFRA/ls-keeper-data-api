using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Messaging.Contracts.Serializers;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = []
)]
[JsonSerializable(typeof(SamImportHoldingMessage))]
[JsonSerializable(typeof(SamImportHolderMessage))]
[JsonSerializable(typeof(CtsImportHoldingMessage))]
[JsonSerializable(typeof(SamBulkScanMessage))]
[JsonSerializable(typeof(CtsBulkScanMessage))]
[JsonSerializable(typeof(CtsUpdateHoldingMessage))]
[JsonSerializable(typeof(CtsUpdateKeeperMessage))]
[JsonSerializable(typeof(CtsUpdateAgentMessage))]
[JsonSerializable(typeof(CtsDailyScanMessage))]
public partial class MessageIdentifierSerializerContext : JsonSerializerContext
{
}