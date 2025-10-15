namespace KeeperData.Tests.Common.Generators;

public static class CommunicationGenerator
{
    private static readonly Random s_random = new();

    public static string? GenerateTelephoneNumber()
    {
        return s_random.Next(2) == 0 ? null : $"020 {s_random.Next(100000, 999999)}";
    }

    public static string? GenerateMobileNumber()
    {
        return s_random.Next(2) == 0 ? null : $"077{s_random.Next(10000000, 99999999)}";
    }

    public static string? GenerateEmail()
    {
        return s_random.Next(2) == 0 ? null : $"email{s_random.Next(100)}@test-email.com";
    }
}