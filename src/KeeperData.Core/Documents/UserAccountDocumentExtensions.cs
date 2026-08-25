using KeeperData.Core.DTOs;

namespace KeeperData.Core.Documents;

/// <summary>
/// Extension methods for mapping UserAccountDocument to DTOs.
/// </summary>
public static class UserAccountDocumentExtensions
{
    public static UserAccountDto ToDto(this UserAccountDocument doc) => new()
    {
        Id = doc.Id,
        Subject = doc.Subject,
        Email = doc.Email,
        FirstName = doc.FirstName,
        LastName = doc.LastName,
        DisplayName = doc.DisplayName,
        AssociationsRefreshedDate = doc.AssociationsRefreshedDate,
        LastUpdatedDate = doc.LastUpdatedDate,
        CphAssociations = doc.CphAssociations?.Select(a => a.ToDto()).ToList() ?? []
    };

    private static CphAssociationDto ToDto(this CphAssociationDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        CphNumber = doc.CphNumber,
        Role = doc.Role,
        PartyId = doc.PartyId,
        HoldingId = doc.HoldingId,
        HoldingName = doc.HoldingName
    };
}
