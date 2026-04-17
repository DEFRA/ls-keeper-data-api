using FluentResults;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Services;

/// <summary>
/// Resolves a raw FCLTY_SUB_BSNSS_ACTVTY_CODE string into a Site Type and zero or more Site Activities.
/// The raw code may contain multiple facility codes separated by commas, spaces, or other delimiters.
/// </summary>
public class SiteTypeDerivedCodeLookupService(
    IReferenceDataCache referenceDataCache,
    ILogger<SiteTypeDerivedCodeLookupService> logger) : ISiteTypeDerivedCodeLookupService
{
    private const string NoMappingFoundCode = "NO_MAPPING_FOUND";
    private const string NoSiteTypeCode = "NO_SITE_TYPE";
    private const string ConflictingSiteTypesCode = "CONFLICTING_SITE_TYPES";

    public SiteTypeDerivedCodeResult? Resolve(string? rawFacilityDerivedCode)
    {
        var result = ResolveInternal(rawFacilityDerivedCode);

        if (!result.IsFailed) return result.Value;
        var error = result.Errors[0];
        var errorCode = error.Metadata.TryGetValue("ErrorCode", out var value)
            ? value?.ToString()
            : "UNKNOWN";

        logger.LogWarning(
            "Failed to resolve facility code '{RawCode}': {ErrorMessage} (Code: {ErrorCode})",
            rawFacilityDerivedCode,
            error.Message,
            errorCode);
        return null;

    }

    private Result<SiteTypeDerivedCodeResult> ResolveInternal(string? rawFacilityDerivedCode)
    {
        if (string.IsNullOrWhiteSpace(rawFacilityDerivedCode))
            return Result.Fail(new Error("Raw facility code is null or whitespace")
                .WithMetadata("ErrorCode", NoMappingFoundCode));

        return FindAllFacilityCodesHits(rawFacilityDerivedCode)
            .Bind(hits => FilterPartialMatches(hits, rawFacilityDerivedCode))
            .Bind(hits => ValidateHasResults(hits, rawFacilityDerivedCode))
            .Bind(hits => ResolveSiteTypeCode(hits, rawFacilityDerivedCode))
            .Bind(context => ValidateSingleSiteType(context, rawFacilityDerivedCode))
            .Map(BuildResult);
    }

    private Result<List<FacilityBusinessActivityMapDocument>> FindAllFacilityCodesHits(string rawFacilityDerivedCode)
    {
        var hits = referenceDataCache.ActivityMaps
            .Where(map => map.IsActive &&
                          !string.IsNullOrWhiteSpace(map.FacilityActivityCode) &&
                          rawFacilityDerivedCode.Contains(map.FacilityActivityCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Result.Ok(hits);
    }

    private Result<List<FacilityBusinessActivityMapDocument>> FilterPartialMatches(
        List<FacilityBusinessActivityMapDocument> hits,
        string rawFacilityDerivedCode)
    {
        if (hits.Count <= 1)
            return Result.Ok(hits);

        var filteredHits = new List<FacilityBusinessActivityMapDocument>();

        foreach (var hit in hits)
        {
            var isPartialMatch = hits.Any(otherHit =>
                otherHit != hit &&
                otherHit.FacilityActivityCode!.Contains(hit.FacilityActivityCode!, StringComparison.OrdinalIgnoreCase));

            if (!isPartialMatch)
            {
                filteredHits.Add(hit);
            }
        }

        if (filteredHits.Count > 1)
        {
            logger.LogInformation(
                "Multiple distinct facility derived code mappings found for raw value '{RawCode}': [{Codes}].",
                rawFacilityDerivedCode,
                string.Join(", ", filteredHits.Select(h => h.FacilityActivityCode)));
        }

        return Result.Ok(filteredHits);
    }

    private static Result<List<FacilityBusinessActivityMapDocument>> ValidateHasResults(
        List<FacilityBusinessActivityMapDocument> hits,
        string rawFacilityDerivedCode)
    {
        return hits.Count > 0
            ? Result.Ok(hits)
            : Result.Fail(new Error($"No facility derived code mapping found for raw value '{rawFacilityDerivedCode}'. Flagged for review.")
                .WithMetadata("ErrorCode", NoMappingFoundCode));
    }

    private static Result<ResolutionContext> ResolveSiteTypeCode(
        List<FacilityBusinessActivityMapDocument> hits,
        string rawFacilityDerivedCode)
    {
        var hitsWithActivity = hits
            .Where(h => !string.IsNullOrWhiteSpace(h.AssociatedSiteActivityCode))
            .ToList();

        var siteTypeCode = hitsWithActivity.Count > 0
            ? hitsWithActivity[0].AssociatedSiteTypeCode
            : hits[0].AssociatedSiteTypeCode;

        if (string.IsNullOrWhiteSpace(siteTypeCode))
        {
            return Result.Fail(new Error($"Facility derived code mapping for '{rawFacilityDerivedCode}' has no associated site type code. Flagged for review.")
                .WithMetadata("ErrorCode", NoSiteTypeCode));
        }

        return Result.Ok(new ResolutionContext(
            hits,
            hitsWithActivity,
            siteTypeCode));
    }

    private static Result<ResolutionContext> ValidateSingleSiteType(
        ResolutionContext context,
        string rawFacilityDerivedCode)
    {
        var allSiteTypeCodes = context.AllHits
            .Select(h => h.AssociatedSiteTypeCode)
            .Where(st => !string.IsNullOrWhiteSpace(st))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allSiteTypeCodes.Count > 1)
        {
            return Result.Fail(new Error($"Multiple facility codes in '{rawFacilityDerivedCode}' map to different site types: [{string.Join(", ", allSiteTypeCodes)}]. Cannot determine which to use.")
                .WithMetadata("ErrorCode", ConflictingSiteTypesCode));
        }

        return Result.Ok(context);
    }

    private SiteTypeDerivedCodeResult BuildResult(ResolutionContext context)
    {
        var siteTypeDoc = referenceDataCache.SiteTypes
            .FirstOrDefault(st => st.Code.Equals(context.SiteTypeCode, StringComparison.OrdinalIgnoreCase));
        var siteTypeName = siteTypeDoc?.Name ?? context.SiteTypeCode;

        var activities = context.HitsWithActivity
            .Where(h => !string.IsNullOrWhiteSpace(h.AssociatedSiteActivityCode))
            .Select(h => h.AssociatedSiteActivityCode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(activityCode =>
            {
                var activityDoc = referenceDataCache.SiteActivityTypes
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
            SiteTypeCode = context.SiteTypeCode,
            SiteTypeName = siteTypeName,
            Activities = activities
        };
    }

    private sealed record ResolutionContext(
        List<FacilityBusinessActivityMapDocument> AllHits,
        List<FacilityBusinessActivityMapDocument> HitsWithActivity,
        string SiteTypeCode);
}