using KeeperData.Core.Domain.Enums;

namespace KeeperData.Core.Domain.Sites.Formatters;

public static class HoldingStatusFormatters
{
    public static string FormatHoldingStatus(bool deleted)
    {
        return deleted
            ? HoldingStatusType.Inactive.ToString()
            : HoldingStatusType.Active.ToString();
    }
}