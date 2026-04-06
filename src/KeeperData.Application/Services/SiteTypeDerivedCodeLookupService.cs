using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Services;

/// <summary>
/// Resolves a raw FCLTY_SUB_BSNSS_ACTVTY_CODE string into a Site Type and zero or more Site Activities
/// by performing substring matching against known facility derived codes from the mapping spreadsheet.
/// </summary>
public class SiteTypeDerivedCodeLookupService(
    IReferenceDataCache cache,
    ILogger<SiteTypeDerivedCodeLookupService> logger) : ISiteTypeDerivedCodeLookupService
{
    private readonly IReferenceDataCache _cache = cache;
    private readonly ILogger<SiteTypeDerivedCodeLookupService> _logger = logger;

    public SiteTypeDerivedCodeResult? Resolve(string? rawFacilityDerivedCode)
    {
        if (string.IsNullOrWhiteSpace(rawFacilityDerivedCode))
            return null;

        var activityMaps = _cache.ActivityMaps;
        if (activityMaps == null || activityMaps.Count == 0)
            return null;

        // Perform substring matching: for each known facility derived code,
        // check if it appears as a substring in the raw value.
        var hits = activityMaps
            .Where(map => map.IsActive &&
                          !string.IsNullOrWhiteSpace(map.FacilityActivityCode) &&
                          rawFacilityDerivedCode.Contains(map.FacilityActivityCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (hits.Count == 0)
        {
            _logger.LogWarning(
                "No facility derived code mapping found for raw value '{RawCode}'. Flagged for review.",
                rawFacilityDerivedCode);
            return null;
        }

        // Try to resolve via an activity mapping first, then fall back to site type only.
        // Hits with an activity code take priority for determining the site type.
        var hitsWithActivity = hits
            .Where(h => !string.IsNullOrWhiteSpace(h.AssociatedSiteActivityCode))
            .ToList();

        string? siteTypeCode;
        string? siteTypeName;

        if (hitsWithActivity.Count > 0)
        {
            // Use the site type from the first activity hit (they should all agree on site type
            // given the spreadsheet structure, but we take the first as canonical).
            siteTypeCode = hitsWithActivity.First().AssociatedSiteTypeCode;
        }
        else
        {
            // No activity derived - use the site type from any hit.
            siteTypeCode = hits.First().AssociatedSiteTypeCode;
        }

        if (string.IsNullOrWhiteSpace(siteTypeCode))
        {
            _logger.LogWarning(
                "Facility derived code mapping for '{RawCode}' has no associated site type code. Flagged for review.",
                rawFacilityDerivedCode);
            return null;
        }

        // Resolve site type name from the SiteTypes reference data cache.
        var siteTypeDoc = _cache.SiteTypes
            .FirstOrDefault(st => st.Code.Equals(siteTypeCode, StringComparison.OrdinalIgnoreCase));
        siteTypeName = siteTypeDoc?.Name ?? siteTypeCode;

        // Collect distinct activities from all hits.
        var activities = hitsWithActivity
            .Where(h => !string.IsNullOrWhiteSpace(h.AssociatedSiteActivityCode))
            .Select(h => h.AssociatedSiteActivityCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(activityCode =>
            {
                var activityDoc = _cache.SiteActivityTypes
                    .FirstOrDefault(at => at.Code.Equals(activityCode, StringComparison.OrdinalIgnoreCase));

                return new SiteTypeDerivedActivityResult
                {
                    Code = activityCode,
                    Name = activityDoc?.Name ?? activityCode
                };
            })
            .ToList();

        return new SiteTypeDerivedCodeResult
        {
            SiteTypeCode = siteTypeCode,
            SiteTypeName = siteTypeName,
            Activities = activities
        };
    }
}
