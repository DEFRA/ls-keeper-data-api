namespace KeeperData.Core.Domain.Sites.Formatters;

public static class AddressFormatters
{
    public static string FormatAddressRange(
        short? saonStart, char? saonStartSuffix,
        short? saonEnd, char? saonEndSuffix,
        short? paonStart, char? paonStartSuffix,
        short? paonEnd, char? paonEndSuffix,
        string? saonDescription = null,
        string? paonDescription = null)
    {
        var parts = new List<string>();

        // SAON
        if (!string.IsNullOrWhiteSpace(saonDescription))
        {
            parts.Add(saonDescription);
        }
        else
        {
            var saonStartFormatted = FormatNumberWithSuffix(saonStart, saonStartSuffix);
            var saonEndFormatted = FormatNumberWithSuffix(saonEnd, saonEndSuffix);

            if (!string.IsNullOrEmpty(saonStartFormatted) && !string.IsNullOrEmpty(saonEndFormatted) && saonStartFormatted != saonEndFormatted)
                parts.Add($"{saonStartFormatted}-{saonEndFormatted}");
            else if (!string.IsNullOrEmpty(saonStartFormatted))
                parts.Add(saonStartFormatted);
        }

        // PAON
        if (!string.IsNullOrWhiteSpace(paonDescription))
        {
            parts.Add(paonDescription);
        }
        else
        {
            var paonStartFormatted = FormatNumberWithSuffix(paonStart, paonStartSuffix);
            var paonEndFormatted = FormatNumberWithSuffix(paonEnd, paonEndSuffix);

            if (!string.IsNullOrEmpty(paonStartFormatted) && !string.IsNullOrEmpty(paonEndFormatted) && paonStartFormatted != paonEndFormatted)
                parts.Add($"{paonStartFormatted}-{paonEndFormatted}");
            else if (!string.IsNullOrEmpty(paonStartFormatted))
                parts.Add(paonStartFormatted);
        }

        return string.Join(", ", parts);
    }

    public static string FormatAddressRange(
        short? saonStart,
        short? saonEnd,
        short? paonStart,
        short? paonEnd,
        string? saonDescription = null,
        string? paonDescription = null)
    {
        var parts = new List<string>();

        // SAON
        if (!string.IsNullOrWhiteSpace(saonDescription))
        {
            parts.Add(saonDescription);
        }
        else if (saonStart.HasValue && saonEnd.HasValue && saonStart != saonEnd)
        {
            parts.Add($"{saonStart}-{saonEnd}");
        }
        else if (saonStart.HasValue)
        {
            parts.Add($"{saonStart}");
        }

        // PAON
        if (!string.IsNullOrWhiteSpace(paonDescription))
        {
            parts.Add(paonDescription);
        }
        else if (paonStart.HasValue && paonEnd.HasValue && paonStart != paonEnd)
        {
            parts.Add($"{paonStart}-{paonEnd}");
        }
        else if (paonStart.HasValue)
        {
            parts.Add($"{paonStart}");
        }

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