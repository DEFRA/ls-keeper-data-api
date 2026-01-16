using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;

namespace KeeperData.Core.Domain.Parties.Formatters;

public static class PartyStatusFormatters
{
    public static string FormatPartyStatus(bool deleted)
    {
        return deleted
            ? PartyStatusType.Inactive.GetDescription() ?? PartyStatusType.Inactive.ToString()
            : PartyStatusType.Active.GetDescription() ?? PartyStatusType.Active.ToString();
    }
}