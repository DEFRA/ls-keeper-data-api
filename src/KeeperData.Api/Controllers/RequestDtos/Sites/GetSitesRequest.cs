using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Sites;

public class GetSitesRequest
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
    /// Filter by multiple site IDs (comma-separated UUIDs). A maximum number of values applies.
    /// </summary>
    [FromQuery] public string? SiteIds { get; set; }

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
    /// The cursor for the next page of results. Leave blank for the first page.
    /// </summary>
    [FromQuery] public string? Cursor { get; set; }
}