namespace KeeperData.Core.DTOs;

/// <summary>
/// A party's role on a holding, read from the normalised SAM read model.
/// </summary>
public class CphAssociationSourceDto
{
    /// <summary>The read model identifier of the party role which grants the association.</summary>
    public required string PartyRoleId { get; set; }

    /// <summary>The CPH number of the holding.</summary>
    public required string CphNumber { get; set; }

    /// <summary>The role held on the holding: owner, holder or keeper.</summary>
    public required string Role { get; set; }

    /// <summary>The SAM identifier of the party holding the role.</summary>
    public required string PartyId { get; set; }

    /// <summary>The read model identifier of the holding.</summary>
    public required string HoldingId { get; set; }

    /// <summary>The name of the holding, where the source carries one.</summary>
    public string? HoldingName { get; set; }
}
