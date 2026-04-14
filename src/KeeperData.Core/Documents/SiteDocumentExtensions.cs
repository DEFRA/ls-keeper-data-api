using KeeperData.Core.DTOs;

namespace KeeperData.Core.Documents;

/// <summary>
/// Extension methods for mapping SiteDocument to DTOs.
/// </summary>
public static class SiteDocumentExtensions
{
    /// <summary>
    /// Maps a SiteDocument to a SiteDto, flattening activities.
    /// </summary>
    public static SiteDto ToDto(this SiteDocument doc) => new()
    {
        Id = doc.Id,
        LastUpdatedDate = doc.LastUpdatedDate,
        Type = doc.Type is not null ? doc.Type.ToDto() : null,
        Name = doc.Name,
        State = doc.State,
        StartDate = doc.StartDate,
        EndDate = doc.EndDate,
        Source = doc.Source,
        DestroyIdentityDocumentsFlag = doc.DestroyIdentityDocumentsFlag,
        Location = doc.Location?.ToDto(),
        Identifiers = doc.Identifiers?.Select(i => i.ToDto()).ToList() ?? [],
        Parties = doc.Parties?.Select(p => p.ToDto()).ToList() ?? [],
        Species = doc.Species?.Select(s => s.ToDto()).ToList() ?? [],
        Marks = doc.Marks?.Select(m => m.ToDto()).ToList() ?? [],
        Activities = doc.Activities?.Select(a => a.ToDto()).ToList() ?? []
    };

    private static SiteTypeSummaryDto ToDto(this SiteTypeSummaryDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        LastUpdatedDate = doc.LastUpdatedDate
    };

    /// <summary>
    /// Flattens a SiteActivityDocument by pulling code and name from the nested Type.
    /// </summary>
    private static SiteActivityDto ToDto(this SiteActivityDocument doc) => new()
    {
        Id = doc.IdentifierId,
        Code = doc.Type.Code,
        Name = doc.Type.Name,
        StartDate = doc.StartDate,
        EndDate = doc.EndDate
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

    private static LocationDto ToDto(this LocationDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        OsMapReference = doc.OsMapReference,
        Easting = doc.Easting,
        Northing = doc.Northing,
        Address = doc.Address?.ToDto(),
        Communication = doc.Communication?.Select(c => c.ToDto()).ToList() ?? [],
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

    private static SitePartyDto ToDto(this SitePartyDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        CustomerNumber = doc.CustomerNumber,
        Title = doc.Title,
        FirstName = doc.FirstName,
        LastName = doc.LastName,
        Name = doc.Name,
        PartyType = doc.PartyType,
        State = doc.State,
        LastUpdatedDate = doc.LastUpdatedDate,
        Communication = doc.Communication?.Select(c => c.ToDto()).ToList() ?? [],
        CorrespondanceAddress = doc.CorrespondanceAddress?.ToDto(),
        PartyRoles = doc.PartyRoles?.Select(r => r.ToDto()).ToList() ?? []
    };

    private static PartyRoleDto ToDto(this PartyRoleDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Role = doc.Role.ToDto(),
        SpeciesManagedByRole = doc.SpeciesManagedByRole?.Select(s => s.ToDto()).ToList() ?? [],
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static RoleDto ToDto(this PartyRoleRoleDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        LastUpdatedDate = doc.LastUpdatedDate
    };

    private static SpeciesSummaryDto ToDto(this SpeciesSummaryDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Code = doc.Code,
        Name = doc.Name,
        LastModifiedDate = doc.LastModifiedDate
    };

    private static GroupMarkDto ToDto(this GroupMarkDocument doc) => new()
    {
        IdentifierId = doc.IdentifierId,
        Mark = doc.Mark,
        StartDate = doc.StartDate,
        EndDate = doc.EndDate,
        Species = doc.Species?.Select(s => s.ToDto()).ToList() ?? [],
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