namespace KeeperData.Core.Domain.Sites.Formatters;

public static class ProductionUsageCodeFormatters
{
    public static string TrimProductionUsageCodeHolding(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return string.Empty;

        var parts = rawCode
            .Trim()
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            > 1 => parts[^1],     // return last segment if dash exists
            1 => parts[0],        // return full code if no dash
            _ => string.Empty
        };
    }

    public static string TrimProductionUsageCodeHerd(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return string.Empty;

        var parts = rawCode
        .Trim()
        .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            >= 2 => parts[1],       // return middle segment if at least two parts
            1 => parts[0],          // fallback to full code if no dash
            _ => string.Empty
        };
    }
}