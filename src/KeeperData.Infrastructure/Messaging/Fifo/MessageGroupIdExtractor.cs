using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;

namespace KeeperData.Infrastructure.Messaging.Fifo;

public static class MessageGroupIdExtractor
{
    // Extracts the appropriate MessageGroupId for FIFO queue grouping.
    public static string ExtractGroupId<T>(T message) where T : MessageType
    {
        return message switch
        {
            // Holding-based messages (CPH grouping)
            SamImportHoldingMessage sam => ForCph(sam.Identifier),
            SamUpdateHoldingMessage sam => ForCph(sam.Identifier),
            CtsImportHoldingMessage cts => ForCph(cts.Identifier.LidIdentifierToCph()),
            CtsUpdateHoldingMessage cts => ForCph(cts.Identifier.LidIdentifierToCph()),

            // Party-based messages (Party ID grouping) - added SamImportHolderMessage
            SamImportHolderMessage holder => ForParty(holder.Identifier),
            CtsUpdateAgentMessage agent => ForParty(agent.Identifier),
            CtsUpdateKeeperMessage keeper => ForParty(keeper.Identifier),

            // System scan messages (Operation-based grouping)
            SamBulkScanMessage => MessageGroupPrefixes.SystemSamBulkScan,
            CtsBulkScanMessage => MessageGroupPrefixes.SystemCtsBulkScan,
            SamDailyScanMessage => MessageGroupPrefixes.SystemSamDailyScan,
            CtsDailyScanMessage => MessageGroupPrefixes.SystemCtsDailyScan,

            _ => throw new NotSupportedException($"Message type {typeof(T).Name} not supported for FIFO grouping")
        };
    }

    private static string ForCph(string cph)
    {
        ValidateNotNullOrWhiteSpace(cph, "Identifier");
        return $"{MessageGroupPrefixes.CountyParishHolding}_{NormalizeCph(cph)}";
    }

    private static string ForParty(string partyId)
    {
        ValidateNotNullOrWhiteSpace(partyId, "Identifier");
        return $"{MessageGroupPrefixes.Party}_{NormalizePartyId(partyId)}";
    }

    private static void ValidateNotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} cannot be null or whitespace.", paramName);
        }
    }

    // Ensure SQS-compatible: alphanumeric + underscores only
    private static string NormalizeCph(string cph)
    {
        return cph.Replace("/", "_").Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
    }

    private static string NormalizePartyId(string partyId)
    {
        return partyId.Replace("@", "_").Replace("#", "_").Replace("&", "_").Replace(":", "_");
    }
}

// Constants for FIFO MessageGroupId prefixes to ensure consistent grouping.
public static class MessageGroupPrefixes
{
    public const string CountyParishHolding = "CPH";
    public const string Party = "PARTY";
    public const string System = "SYSTEM";

    // System operation constants - specific for each scan type to allow proper grouping
    public const string SystemSamBulkScan = "SYSTEM_SAM_BULK_SCAN";
    public const string SystemCtsBulkScan = "SYSTEM_CTS_BULK_SCAN";
    public const string SystemSamDailyScan = "SYSTEM_SAM_DAILY_SCAN";
    public const string SystemCtsDailyScan = "SYSTEM_CTS_DAILY_SCAN";
}