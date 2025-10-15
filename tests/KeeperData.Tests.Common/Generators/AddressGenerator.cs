namespace KeeperData.Tests.Common.Generators;

public static class AddressGenerator
{
    private static readonly Random s_random = new();

    public static (string addressName, string? address2, string? address3, string? address4, string? address5, string? postCode) GenerateCtsAddress()
    {
        var addressName = Guid.NewGuid().ToString();
        var address2 = $"{s_random.Next(1, 999)} North Market";
        var address3 = $"{s_random.Next(1, 999)} North Oxford";
        var address4 = $"{s_random.Next(1, 999)} Market Square";
        var address5 = $"{s_random.Next(1, 999)} Oxford";
        var postcode = s_random.Next(2) == 0 ? null : $"OX{s_random.Next(10, 99)} {(s_random.Next(1, 9))}XY";

        return (
            addressName,
            address2,
            address3,
            address4,
            address5,
            postcode);
    }

    public static string? GenerateMapReference()
    {
        return s_random.Next(2) == 0 ? null : $"NN {s_random.Next(100, 999)} {s_random.Next(100, 999)}";
    }
}