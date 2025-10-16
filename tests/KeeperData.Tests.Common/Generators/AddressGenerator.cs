namespace KeeperData.Tests.Common.Generators;

public static class AddressGenerator
{
    private static readonly Random s_random = new();

    public static (string addressName, string? address2, string? address3, string? address4, string? address5, string? postCode) GenerateCtsAddress(bool allowNulls = true)
    {
        var addressName = Guid.NewGuid().ToString();
        var address2 = allowNulls && s_random.Next(2) == 0 ? null : $"{s_random.Next(1, 999)} North Market";
        var address3 = allowNulls && s_random.Next(2) == 0 ? null : $"{s_random.Next(1, 999)} North Oxford";
        var address4 = allowNulls && s_random.Next(2) == 0 ? null : $"{s_random.Next(1, 999)} Market Square";
        var address5 = allowNulls && s_random.Next(2) == 0 ? null : $"{s_random.Next(1, 999)} Oxford";
        var postcode = allowNulls && s_random.Next(2) == 0 ? null : $"OX{s_random.Next(10, 99)} {(s_random.Next(1, 9))}XY";

        return (
            addressName,
            address2,
            address3,
            address4,
            address5,
            postcode);
    }

    public static (
        short? saonStart,
        char? saonStartSuffix,
        short? saonEnd,
        char? saonEndSuffix,
        short? paonStart,
        char? paonStartSuffix,
        short? paonEnd,
        char? paonEndSuffix,
        string? street,
        string? town,
        string? locality,
        string? postcode,
        string? countryCode,
        string? ukInternalCode
    ) GenerateSamAddress(bool allowNulls = true)
    {
        var street = allowNulls && s_random.Next(2) == 0 ? null : $"{s_random.Next(1, 999)} North Market, Market Square";
        var town = allowNulls && s_random.Next(2) == 0 ? null : $"{s_random.Next(1, 999)} Oxford";
        var locality = allowNulls && s_random.Next(2) == 0 ? null : $"{s_random.Next(1, 999)} North Oxford";
        var postcode = allowNulls && s_random.Next(2) == 0 ? null : $"OX{s_random.Next(10, 99)} {(s_random.Next(1, 9))}XY";
        var countryCode = allowNulls && s_random.Next(2) == 0 ? null : "GB";
        var ukInternalCode = allowNulls && s_random.Next(2) == 0 ? null : "GB";

        var saonStart = allowNulls && s_random.Next(2) == 0 ? null : (short?)s_random.Next(1, 20);
        var saonStartSuffix = saonStart.HasValue && (!allowNulls || s_random.Next(2) == 1) ? (char?)((char)('A' + s_random.Next(0, 5))) : null;
        var saonEnd = saonStart.HasValue && (!allowNulls || s_random.Next(2) == 1) ? (short?)(saonStart.Value + s_random.Next(1, 3)) : null;
        var saonEndSuffix = saonEnd.HasValue && (!allowNulls || s_random.Next(2) == 1) ? (char?)((char)('A' + s_random.Next(0, 5))) : null;

        var paonStart = (short)s_random.Next(1, 999);
        var paonStartSuffix = allowNulls && s_random.Next(3) == 0 ? null : (char?)((char)('A' + s_random.Next(0, 5)));
        var paonEnd = allowNulls && s_random.Next(3) == 0 ? null : (short?)(paonStart + s_random.Next(1, 5));
        var paonEndSuffix = paonEnd.HasValue && (!allowNulls || s_random.Next(2) == 1) ? (char?)((char)('A' + s_random.Next(0, 5))) : null;

        return (
            saonStart,
            saonStartSuffix,
            saonEnd,
            saonEndSuffix,
            paonStart,
            paonStartSuffix,
            paonEnd,
            paonEndSuffix,
            street,
            town,
            locality,
            postcode,
            countryCode,
            ukInternalCode);
    }

    public static string? GenerateMapReference(bool allowNulls = true)
    {
        return allowNulls && s_random.Next(2) == 0 ? null : $"NN {s_random.Next(100, 999)} {s_random.Next(100, 999)}";
    }
}
