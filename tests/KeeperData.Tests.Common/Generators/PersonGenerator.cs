namespace KeeperData.Tests.Common.Generators;

public static class PersonGenerator
{
    private static readonly Random s_random = new();

    private static readonly string[] s_titles = ["Mr", "Mrs", "Miss", "Ms", "Dr"];

    private static readonly string[] s_initials = ["A", "B", "C", "D", "E", "J", "K", "L", "M", "N"];

    private static readonly string[] s_forenames = [
        "James", "Emily", "Oliver", "Sophie", "Jack",
        "Amelia", "Harry", "Isla", "George", "Mia",
        "Thomas", "Grace", "Charlie", "Evie", "Oscar"
    ];

    private static readonly string[] s_surnames = [
        "Smith", "Jones", "Taylor", "Brown", "Williams", "Davies", "Evans", "Wilson", "Thomas", "Roberts"
    ];

    public static (string? title, string? initials, string? forename, string? surname) GeneratePerson(bool allowNull = true)
    {
        var title = allowNull && s_random.Next(2) == 0 ? null : s_titles[s_random.Next(s_titles.Length)];
        var initials = allowNull && s_random.Next(2) == 0 ? null : s_initials[s_random.Next(s_initials.Length)];
        var forename = allowNull && s_random.Next(2) == 0 ? null : s_forenames[s_random.Next(s_forenames.Length)];
        var surname = allowNull && s_random.Next(2) == 0 ? null : s_surnames[s_random.Next(s_surnames.Length)];

        return (
            title,
            initials,
            forename,
            surname);
    }
}