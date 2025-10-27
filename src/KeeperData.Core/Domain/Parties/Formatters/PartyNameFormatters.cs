namespace KeeperData.Core.Domain.Parties.Formatters;

public static class PartyNameFormatters
{
    public static string FormatPartyFirstName(string? givenName, string? givenName2)
    {
        return string.Join(" ", new[] { givenName, givenName2 }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    public static string FormatPartyFullName(
        string? organisationName,
        string? title,
        string? givenName,
        string? givenName2,
        string? familyName)
    {
        if (!string.IsNullOrWhiteSpace(organisationName))
        {
            return organisationName;
        }

        var parts = new[] { title, givenName, givenName2, familyName }
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(" ", parts);
    }
}