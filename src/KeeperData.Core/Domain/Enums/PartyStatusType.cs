using System.ComponentModel;

namespace KeeperData.Core.Domain.Enums;

public enum PartyStatusType
{
    [Description("inactive")]
    Inactive = 0,

    [Description("active")]
    Active
}