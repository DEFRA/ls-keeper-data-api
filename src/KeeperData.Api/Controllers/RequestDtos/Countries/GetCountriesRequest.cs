using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Countries;

public class GetCountriesRequest
{
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }
    /// <summary>
    /// The field to order the results by. Available fields for sorting: name, code. Defaults to code.
    /// </summary>
    [FromQuery] public string? Order { get; set; }

    /// <summary>
    /// The sort direction. Available values: asc, desc. Defaults to asc.
    /// </summary>
    [FromQuery] public string? Sort { get; set; }
    [FromQuery] public string? Name { get; set; }
    [FromQuery] public string? Code { get; set; }
    [FromQuery] public bool? EuTradeMember { get; set; }
    [FromQuery] public bool? DevolvedAuthority { get; set; }

    /// <summary>
    /// Returns only records that have been updated since the provided timestamp (greater than or equal to). This is the key parameter for Change Data Capture (CDC).
    /// </summary>
    [FromQuery] public DateTime? LastUpdatedDate { get; internal set; }
}