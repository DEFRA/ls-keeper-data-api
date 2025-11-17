namespace KeeperData.Tests.Common.Generators;

public class RoleGenerator
{
    private static readonly Random s_random = new();

    private static readonly string[] s_roles = ["Agent", "Keeper", "Owner", "Veterinary Contact",
        "Slaughterhouse Liaison", "Common Land Representative", "Calf Collection Coordinator",
        "Holding Manager", "Biosecurity Office", "Movement Notifier", "Emergency Contact",
        "Trading Partner", "Inspection Contact", "Data Submitter", "Holding Administrator"];

    public static string? GenerateRole(bool allowNulls = false)
    {
        return allowNulls && s_random.Next(2) == 0 ? null : s_roles[s_random.Next(s_roles.Length)];
    }

    public static List<string?> GenerateRoles(int count, bool allowDuplicates = false, bool allowNulls = false)
    {
        var roles = new List<string?>();

        while (roles.Count < count)
        {
            var role = allowNulls && s_random.Next(2) == 0 ? null : s_roles[s_random.Next(s_roles.Length)];

            if (!allowDuplicates && role != null && roles.Contains(role))
                continue;

            roles.Add(role);
        }

        return roles;
    }
}