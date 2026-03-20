using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Countries;

public class GetCountriesRequest
{
    /// <summary>
    /// Page number (1-based). Defaults to 1.
    /// </summary>
    [FromQuery] public int? Page { get; set; }

    /// <summary>
    /// Number of records per page. Defaults to 10.
    /// </summary>
    [FromQuery] public int? PageSize { get; set; }

    /// <summary>
    /// The field to order the results by. Available fields for sorting: name, code. Defaults to code.
    /// </summary>
    [FromQuery] public string? Order { get; set; }

    /// <summary>
    /// The sort direction. Available values: asc, desc. Defaults to asc.
    /// </summary>
    [FromQuery] public string? Sort { get; set; }

    /// <summary>
    /// Filter countries by name.
    /// </summary>
    [FromQuery] public string? Name { get; set; }

    /// <summary>
    /// Filter countries by code. Accepts a comma-separated list of country codes.
    /// </summary>
    [FromQuery] public string? Code { get; set; }

    /// <summary>
    /// Filter by EU trade membership flag.
    /// </summary>
    [FromQuery] public bool? EuTradeMember { get; set; }

    /// <summary>
    /// Filter by devolved authority flag.
    /// </summary>
    [FromQuery] public bool? DevolvedAuthority { get; set; }

    /// <summary>
    /// Returns only records that have been updated since the provided timestamp (greater than or equal to). This is the key parameter for Change Data Capture (CDC).
    /// </summary>
    [FromQuery] public DateTime? LastUpdatedDate { get; internal set; }
}