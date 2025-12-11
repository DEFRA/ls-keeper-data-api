namespace KeeperData.Core.Domain.Sites.Formatters;

public class PremiseActivityTypeFormatters
{
    public static string TrimFacilityActivityCode(string? rawCode)
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
}