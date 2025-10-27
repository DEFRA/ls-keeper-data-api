using System.ComponentModel;

namespace KeeperData.Core.Domain.Enums;

/// <summary>
/// Descriptions (TBC) to match with seeded values when available.
/// </summary>
public enum InferredRoleType
{
    [Description("Agent")]
    Agent = 1,

    [Description("Keeper")]
    PrimaryKeeper,

    [Description("Holder")]
    Holder
}