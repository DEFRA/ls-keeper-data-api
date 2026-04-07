namespace KeeperData.Core.Services;

/// <summary>
/// Resolves a raw FCLTY_SUB_BSNSS_ACTVTY_CODE string into a Site Type and zero or more Site Activities
/// by performing substring matching against known facility derived codes from the mapping spreadsheet.
/// </summary>
public interface ISiteTypeDerivedCodeLookupService
{
    /// <summary>
    /// Resolves a raw facility derived code string into a site type code and a set of distinct activity codes.
    /// The raw string may contain multiple concatenated codes (not comma-separated).
    /// Each known facility derived code is checked as a substring of the raw value.
    /// </summary>
    /// <param name="rawFacilityDerivedCode">The raw FCLTY_SUB_BSNSS_ACTVTY_CODE value from SAM data.</param>
    /// <returns>A result containing the resolved site type code/name and a list of distinct activity codes/names.
    /// Returns null if no mapping is found.</returns>
    SiteTypeDerivedCodeResult? Resolve(string? rawFacilityDerivedCode);
}

/// <summary>
/// The result of resolving a facility derived code to a site type and activities.
/// </summary>
public class SiteTypeDerivedCodeResult
{
    /// <summary>
    /// The resolved site type code.
    /// </summary>
    public required string SiteTypeCode { get; init; }

    /// <summary>
    /// The resolved site type name.
    /// </summary>
    public required string SiteTypeName { get; init; }

    /// <summary>
    /// The distinct site activities resolved from the derived code.
    /// May be empty if the derived code maps only to a site type with no activities.
    /// </summary>
    public List<SiteTypeDerivedActivityResult> Activities { get; init; } = [];
}

/// <summary>
/// A single activity resolved from a facility derived code.
/// </summary>
public class SiteTypeDerivedActivityResult
{
    /// <summary>
    /// The site activity code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// The site activity name.
    /// </summary>
    public required string Name { get; init; }
}