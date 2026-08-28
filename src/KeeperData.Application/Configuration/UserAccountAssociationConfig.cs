namespace KeeperData.Application.Configuration;

/// <summary>
/// Controls how a user account's CPH association snapshot is derived from the SAM read model.
/// </summary>
public class UserAccountAssociationConfig
{
    public static readonly string SectionName = "UserAccountAssociations";

    /// <summary>
    /// The read model party roles which grant a CPH association. The read model constrains roles to
    /// owner, holder and keeper; owner is the equivalent of the LIVESTOCKOWNER scope.
    /// </summary>
    public string[] Roles { get; set; } = ["owner"];
}