namespace KeeperData.Tests.Common.Generators;

public class IntervalGenerator
{
    private static readonly Random s_random = new();

    private static readonly string[] s_internalUnits = ["Weeks", "Months"];

    public static (decimal? interval, string? intervalUnit) GenerateInterval(bool allowNulls = false)
    {
        decimal? interval = allowNulls && s_random.Next(2) == 0 ? null : s_random.Next(1, 12);
        var intervalUnit = allowNulls && s_random.Next(2) == 0 ? null : s_internalUnits[s_random.Next(s_internalUnits.Length)];

        return (interval, intervalUnit);
    }
}
