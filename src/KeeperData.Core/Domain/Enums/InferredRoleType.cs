using System.ComponentModel;

namespace KeeperData.Core.Domain.Enums;

public enum InferredRoleType
{
    [Description("Agent - Name tbc to match with seed")]
    Agent = 1,

    [Description("Keeper - Name tbc to match with seed")]
    PrimaryKeeper
}
