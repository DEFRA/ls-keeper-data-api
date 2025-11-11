namespace KeeperData.Tests.Common.Generators;

public static class CphGenerator
{
    private static readonly Random s_random = new();

    public static string GenerateFormattedCph()
    {
        var raw = GenerateRawCph();
        return FormatCph(raw);
    }

    public static string GenerateCtsFormattedLidIdentifier(string prefix)
    {
        var cph = GenerateFormattedCph();
        return $"{prefix}-{cph}";
    }

    public static string GenerateFormattedCphh(string suffix)
    {
        var cph = GenerateFormattedCph();
        return $"{cph}-{suffix}";
    }

    private static string GenerateRawCph()
    {
        return $"{s_random.Next(10, 99)}{s_random.Next(100, 999)}{s_random.Next(1000, 9999)}";
    }

    private static string FormatCph(string rawCph)
    {
        var county = rawCph[..2];
        var parish = rawCph.Substring(2, 3);
        var holding = rawCph.Substring(5, 4);

        return $"{county}/{parish}/{holding}";
    }
}