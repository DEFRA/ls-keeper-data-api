namespace KeeperData.Core.Entities.ReadModel;

/// <summary>
/// The relationship which grants a party access to a holding. Role is one of owner, holder or keeper.
/// </summary>
public class PartyRoleEntity
{
    public string Id { get; set; } = string.Empty;
    public string PartyId { get; set; } = string.Empty;
    public string HoldingId { get; set; } = string.Empty;
    public string? HerdId { get; set; }
    public string Role { get; set; } = string.Empty;
}
