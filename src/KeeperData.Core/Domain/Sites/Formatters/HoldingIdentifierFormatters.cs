namespace KeeperData.Core.Domain.Sites.Formatters;

public static class HoldingIdentifierFormatters
{
    public static string CphhToCph(this string? cphh)
    {
        if (string.IsNullOrWhiteSpace(cphh))
            return string.Empty;

        var dashIndex = cphh.IndexOf('-');
        return dashIndex >= 0 ? cphh.Substring(0, dashIndex) : cphh;
    }
}