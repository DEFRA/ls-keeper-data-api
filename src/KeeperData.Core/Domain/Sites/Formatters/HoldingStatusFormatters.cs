using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;

namespace KeeperData.Core.Domain.Sites.Formatters;

public static class HoldingStatusFormatters
{
    public static string FormatHoldingStatus(bool deleted)
    {
        return deleted
            ? HoldingStatusType.Inactive.GetDescription() ?? HoldingStatusType.Inactive.ToString()
            : HoldingStatusType.Active.GetDescription() ?? HoldingStatusType.Active.ToString();
    }
}