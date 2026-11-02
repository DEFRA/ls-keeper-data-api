using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KeeperData.Api.Controllers.RequestDtos.Sites;

public class GetSitesRequest : IValidatableObject
{
    /// <summary>
    /// Filter by a single site identifier such as CPH Number, FSA No, Port No etc.
    /// </summary>
    [FromQuery] public string? SiteIdentifier { get; set; }

    /// <summary>
    /// Filter by multiple site identifiers (comma-separated). A maximum number of values applies.
    /// </summary>
    [FromQuery] public string? SiteIdentifiers { get; set; }

    /// <summary>
    /// Filter by site type code (comma-separated). Possible site type codes include:
    /// AC (Assembly Centre), AH (Agricultural Holding), AP (Airport), CA (Calf Collection Centre),
    /// CC (Collection Centre), CL (Common Land), HA (Haulier), HK (Hunt Kennel),
    /// KY (Knacker's Yard), LA (Lairage), LR (Laboratory/Research), MA (Market),
    /// QU (Quarantine), SG (Showground), SM (S/H Both MP and Cs), SP (Seaport),
    /// SR (Slaughter House Red Meat), ST (Staging Post), SW (Slaughter House White Meat),
    /// VE (Veterinary Practice), VI (Veterinary Investigation Centre), WP (Wildlife Park), ZO (Zoo).
    /// </summary>
    [FromQuery] public string? Type { get; set; }

    /// <summary>
    /// Filter by a single site ID (UUID).
    /// </summary>
    [FromQuery] public Guid? SiteId { get; set; }

    /// <summary>
    /// Filter by multiple site IDs (comma-separated UUIDs). Whitespace around each value is ignored. A maximum number of values applies.
    /// If any value is not a valid UUID, or no values are supplied (for example only commas or whitespace), the request is rejected with 400 Bad Request.
    /// </summary>
    // Keep blank values so a supplied but empty filter is rejected rather than treated as omitted.
    [FromQuery]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? SiteIds { get; set; }

    /// <summary>
    /// Filter sites by a keeper party ID (UUID).
    /// </summary>
    [FromQuery] public Guid? KeeperPartyId { get; set; }

    /// <summary>
    /// Returns only records that have been updated since the provided timestamp (greater than or equal to). This is the key parameter for Change Data Capture (CDC).
    /// </summary>
    [FromQuery] public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    [FromQuery] public int? Page { get; set; }

    /// <summary>
    /// Number of records per page. Defaults to 10.
    /// </summary>
    [FromQuery] public int? PageSize { get; set; }

    /// <summary>
    /// The field to order the results by. Available fields for sorting: name, type, siteidentifier. Defaults to name.
    /// </summary>
    [FromQuery] public string? Order { get; set; }

    /// <summary>
    /// The sort direction. Available values: asc, desc. Defaults to asc.
    /// </summary>
    [FromQuery] public string? Sort { get; set; }

    /// <summary>
    /// The cursor for the next page of results, taken from the nextCursor value of a previous response. Leave blank for the first page.
    /// When supplied, the cursor takes precedence over page. A malformed cursor is rejected with 400 Bad Request.
    /// </summary>
    [FromQuery] public string? Cursor { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SiteIds is not null)
        {
            var values = SplitSiteIds(SiteIds);

            if (values.Count == 0)
            {
                yield return new ValidationResult("SiteIds must contain at least one UUID when supplied.", [nameof(SiteIds)]);
            }
            else if (values.Any(v => !Guid.TryParse(v, out _)))
            {
                yield return new ValidationResult("SiteIds must be a comma-separated list of valid UUIDs.", [nameof(SiteIds)]);
            }
        }
    }

    /// <summary>
    /// Parses <see cref="SiteIds"/> into GUIDs. Only call after the request has passed validation.
    /// </summary>
    public List<Guid>? ParseSiteIds() =>
        SiteIds is null ? null : SplitSiteIds(SiteIds).Select(Guid.Parse).ToList();

    private static List<string> SplitSiteIds(string siteIds) =>
        siteIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}