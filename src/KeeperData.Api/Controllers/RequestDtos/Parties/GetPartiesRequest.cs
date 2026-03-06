using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Parties;

public class GetPartiesRequest
{
    [FromQuery] public string? FirstName { get; set; }
    [FromQuery] public string? LastName { get; set; }
    [FromQuery] public string? Email { get; set; }
    /// <summary>
    /// Returns only records that have been updated since the provided timestamp (greater than or equal to). This is the key parameter for Change Data Capture (CDC).
    /// </summary>
    [FromQuery] public DateTime? LastUpdatedDate { get; set; }
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }
    /// <summary>
    /// The field to order the results by. Available fields for sorting: id (sorts by id), name. Defaults to name.
    /// </summary>
    [FromQuery] public string? Order { get; set; }

    /// <summary>
    /// The sort direction. Available values: asc, desc. Defaults to asc.
    /// </summary>
    [FromQuery] public string? Sort { get; set; }
}