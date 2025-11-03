using System.Text.Json.Serialization;

namespace DataConverter.Models;

public record CountryJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("longName")] string LongName,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("euTradeMember")] bool EuTradeMember,
    [property: JsonPropertyName("devolvedAuthority")] bool DevolvedAuthority,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);

public record SpeciesJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);

public record PartyRoleJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);

public record PremisesTypeJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);

public record PremisesActivityTypeJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("priorityOrder")] int PriorityOrder,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);

public record SiteIdentifierTypeJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);