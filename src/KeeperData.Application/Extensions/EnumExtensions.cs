using System.ComponentModel;
using System.Reflection;

namespace KeeperData.Application.Extensions;

public static class EnumExtensions
{
    public static string? GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description;
    }
}
