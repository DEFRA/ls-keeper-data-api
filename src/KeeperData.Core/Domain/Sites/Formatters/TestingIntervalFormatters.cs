using System.Globalization;

namespace KeeperData.Core.Domain.Sites.Formatters;

public static class TestingIntervalFormatters
{
    public static string? FormatTbTestingInterval(decimal? interval, string? unit)
    {
        if (interval is null)
            return null;

        if (string.IsNullOrWhiteSpace(unit))
            return interval.Value.ToString();

        var normalizedUnit = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(unit.Trim().ToLowerInvariant());
        return $"{interval.Value} {normalizedUnit}";
    }
}