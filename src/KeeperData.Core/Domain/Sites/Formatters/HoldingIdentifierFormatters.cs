namespace KeeperData.Core.Domain.Sites.Formatters;

public static class HoldingIdentifierFormatters
{
    public static string CphhToCph(this string? cphh)
    {
        if (string.IsNullOrWhiteSpace(cphh))
            return string.Empty;

        var dashIndex = cphh.IndexOf('-');
        return dashIndex >= 0 ? cphh[..dashIndex] : cphh;
    }

    public static string LidIdentifierToCph(this string? lidIdentifier)
    {
        if (string.IsNullOrWhiteSpace(lidIdentifier))
            return string.Empty;

        var dashIndex = lidIdentifier.IndexOf('-');
        return dashIndex >= 0 ? lidIdentifier[(dashIndex + 1)..] : lidIdentifier;
    }
}