using KeeperData.Core.Domain.Enums;

namespace KeeperData.Core.Domain.Sites.Formatters;

public static class HoldingStatusFormatters
{
    public static string FormatHoldingStatus(DateTime? endDate)
    {
        return endDate.HasValue && endDate != default(DateTime)
            ? HoldingStatusType.Inactive.ToString()
            : HoldingStatusType.Active.ToString();
    }
}