using System.ComponentModel;

namespace KeeperData.Core.Domain.Enums;

public enum HoldingIdentifierType
{
    [Description("CPH Number")]
    CPHN = 1,

    [Description("FSA Number")]
    FSAN,

    [Description("Port Number")]
    PRTN
}