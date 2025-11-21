using KeeperData.Core.Domain.Enums;

namespace KeeperData.Core.Domain.Parties.Formatters;

public static class PartyStatusFormatters
{
    public static string FormatPartyStatus(bool deleted)
    {
        return deleted
            ? HoldingStatusType.Inactive.ToString()
            : HoldingStatusType.Active.ToString();
    }
}