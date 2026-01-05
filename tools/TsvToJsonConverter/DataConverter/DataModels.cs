using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TsvToJsonConverter.DataConverter;

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
public record RoleJson(
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

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
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

[ExcludeFromCodeCoverage]
public record ProductionUsageJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);