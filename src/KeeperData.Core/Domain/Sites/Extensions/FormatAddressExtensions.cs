namespace KeeperData.Core.Domain.Sites.Extensions;

public static class FormatAddressExtensions
{
    public static string FormatAddressRange(
        short? saonStart, char? saonStartSuffix,
        short? saonEnd, char? saonEndSuffix,
        short? paonStart, char? paonStartSuffix,
        short? paonEnd, char? paonEndSuffix,
        string saonLabel = "")
    {
        var parts = new List<string>();

        var saonStartFormatted = FormatNumberWithSuffix(saonStart, saonStartSuffix);
        var saonEndFormatted = FormatNumberWithSuffix(saonEnd, saonEndSuffix);

        if (!string.IsNullOrEmpty(saonStartFormatted) && !string.IsNullOrEmpty(saonEndFormatted) && saonStartFormatted != saonEndFormatted)
            parts.Add(AddSaonLabel($"{saonStartFormatted}-{saonEndFormatted}", saonLabel));
        else if (!string.IsNullOrEmpty(saonStartFormatted))
            parts.Add(AddSaonLabel($"{saonStartFormatted}", saonLabel));

        var paonStartFormatted = FormatNumberWithSuffix(paonStart, paonStartSuffix);
        var paonEndFormatted = FormatNumberWithSuffix(paonEnd, paonEndSuffix);

        if (!string.IsNullOrEmpty(paonStartFormatted) && !string.IsNullOrEmpty(paonEndFormatted) && paonStartFormatted != paonEndFormatted)
            parts.Add($"{paonStartFormatted}-{paonEndFormatted}");
        else if (!string.IsNullOrEmpty(paonStartFormatted))
            parts.Add($"{paonStartFormatted}");

        return string.Join(", ", parts);
    }

    public static string FormatAddressRange(
        short? saonStart,
        short? saonEnd,
        short? paonStart,
        short? paonEnd,
        string saonLabel = "")
    {
        var parts = new List<string>();

        // SAON (e.g., Flat 1-3)
        if (saonStart.HasValue && saonEnd.HasValue && saonStart != saonEnd)
            parts.Add(AddSaonLabel($"{saonStart}-{saonEnd}", saonLabel));
        else if (saonStart.HasValue)
            parts.Add(AddSaonLabel($"{saonStart}", saonLabel));

        // PAON (e.g., 10-12)
        if (paonStart.HasValue && paonEnd.HasValue && paonStart != paonEnd)
            parts.Add($"{paonStart}-{paonEnd}");
        else if (paonStart.HasValue)
            parts.Add($"{paonStart}");

        return string.Join(", ", parts);
    }

    private static string FormatNumberWithSuffix(short? number, char? suffix)
    {
        if (!number.HasValue)
            return string.Empty;

        return suffix.HasValue
            ? $"{number}{suffix}"
            : number.ToString()!;
    }

    private static string AddSaonLabel(string addressToken, string saonLabel = "")
    {
        if (string.IsNullOrWhiteSpace(saonLabel))
            return addressToken;

        return $"{saonLabel} {addressToken}";
    }
}