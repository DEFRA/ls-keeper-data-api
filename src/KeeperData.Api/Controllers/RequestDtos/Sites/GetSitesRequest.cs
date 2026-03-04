using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Sites;

public class GetSitesRequest
{
    [FromQuery] public string? SiteIdentifier { get; set; }
    [FromQuery] public string? SiteIdentifiers { get; set; }
    [FromQuery] public string? Type { get; set; }
    [FromQuery] public Guid? SiteId { get; set; }
    [FromQuery] public string? SiteIds { get; set; }
    [FromQuery] public Guid? KeeperPartyId { get; set; }
    /// <summary>
    /// Returns only records that have been updated since the provided timestamp (greater than or equal to). This is the key parameter for Change Data Capture (CDC).
    /// </summary>
    [FromQuery] public DateTime? LastUpdatedDate { get; set; }
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }
    [FromQuery] public string? Order { get; set; }
    [FromQuery] public string? Sort { get; set; }
}