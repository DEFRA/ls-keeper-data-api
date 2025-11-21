using System.ComponentModel;

namespace KeeperData.Core.Domain.Enums;

public enum InferredRoleType
{
    [Description("AGENT")]
    Agent = 1,

    [Description("LIVESTOCKKEEPER")]
    LivestockKeeper,

    [Description("CPHHOLDER")]
    CphHolder,

    [Description("LIVESTOCKOWNER")]
    LivestockOwner
}