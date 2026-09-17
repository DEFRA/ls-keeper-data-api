namespace KeeperData.Core.DTOs;

public sealed record HoldingDetail(
    string Identifier,
    string? HoldingType,
    string? Name,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    HoldingLocation Location,
    IReadOnlyList<HoldingAssociation> Associations,
    IReadOnlyList<string> AllowedSpecies,
    IReadOnlyList<HoldingMark> Marks);

public sealed record HoldingLocation(
    string? OsMapReference,
    int? Easting,
    int? Northing,
    HoldingAddress Address);

public sealed record HoldingAddress(
    string? Udprn,
    string? AddressLine1,
    string? AddressLine2,
    string? PostTown,
    string? Locality,
    string? Postcode,
    string? Country);

public sealed record HoldingAssociation(
    string CustomerNumber,
    string? Title,
    string? FirstName,
    string? LastName,
    string? Name,
    string PartyType,
    string? Email,
    string? Mobile,
    string? Telephone,
    IReadOnlyList<HoldingRole> Roles);

public sealed record HoldingRole(
    string Code,
    IReadOnlyList<string> Species);

public sealed record HoldingMark(
    string Mark,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    IReadOnlyList<string> Species);