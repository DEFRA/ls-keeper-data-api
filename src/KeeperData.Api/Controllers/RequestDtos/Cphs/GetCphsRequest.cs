using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Cphs;

public class GetCphsRequest
{
    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    [FromQuery] public int? Page { get; set; }

    /// <summary>
    /// Number of records per page. Defaults to 10, maximum 100.
    /// </summary>
    [FromQuery] public int? PageSize { get; set; }

    /// <summary>
    /// The field to order the results by. Available fields: cph. Defaults to cph.
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