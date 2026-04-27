using KeeperData.Core.DTOs;

namespace KeeperData.Core.Documents;

/// <summary>
/// Extension methods for mapping PartyDocument to DTOs.
/// </summary>
public static class PartyDocumentExtensions
{
    /// <summary>
    /// Maps a PartyDocument to a PartyDto.
    /// </summary>
    public static PartyDto ToDto(this PartyDocument doc) => new()
    {
        Id = doc.Id,
        LastUpdatedDate = doc.LastUpdatedDate,
        Title = doc.Title,
        FirstName = doc.FirstName,
        LastName = doc.LastName,
        Name = doc.Name,
        CustomerNumber = doc.CustomerNumber,
        PartyType = doc.PartyType,
        State = doc.State,
        Communication = doc.Communication?.Select(c => c.ToDto()).ToList() ?? [],
        CorrespondenceAddress = doc.CorrespondanceAddress?.ToDto(),
        PartyRoles = doc.PartyRoles?.Select(r => r.ToDto()).ToList() ?? []
    };

    private static PartyRoleWithSiteDto ToDto(this PartyRoleWithSiteDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Site = doc.Site?.ToDto(),
        Role = doc.Role.ToDto(),
        SpeciesManagedByRole = doc.SpeciesManagedByRole?.Select(s => s.ToDto()).ToList() ?? [],
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static PartyRoleSiteDto ToDto(this PartyRoleSiteDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Name = doc.Name,
        Type = doc.Type?.ToDto(),
        State = doc.State,
        Identifiers = doc.Identifiers?.Select(i => i.ToDto()).ToList() ?? [],
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static SiteTypeSummaryDto ToDto(this SiteTypeSummaryDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static SiteIdentifierDto ToDto(this SiteIdentifierDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Identifier = doc.Identifier,
        Type = doc.Type.ToDto(),
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static SiteIdentifierTypeDto ToDto(this SiteIdentifierSummaryDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static AddressDto ToDto(this AddressDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Uprn = doc.Uprn,
        AddressLine1 = doc.AddressLine1,
        AddressLine2 = doc.AddressLine2,
        PostTown = doc.PostTown,
        County = doc.County,
        Postcode = doc.Postcode,
        Country = doc.Country?.ToDto(),
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static CountrySummaryDto ToDto(this CountrySummaryDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        LongName = doc.LongName,
        EuTradeMemberFlag = doc.EuTradeMemberFlag,
        DevolvedAuthorityFlag = doc.DevolvedAuthorityFlag,
        LastModifiedDate = doc.LastModifiedDate
    };

    private static CommunicationDto ToDto(this CommunicationDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Email = doc.Email,
        Mobile = doc.Mobile,
        Landline = doc.Landline,
        PrimaryContactFlag = doc.PrimaryContactFlag,
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static RoleDto ToDto(this PartyRoleRoleDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static ManagedSpeciesDto ToDto(this ManagedSpeciesDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        StartDate = doc.StartDate,
        EndDate = doc.EndDate,
        LastUpdatedDate = doc.LastUpdatedDate
    };
}
