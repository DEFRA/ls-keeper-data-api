using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Parties;

public class GetCountriesRequest
{
    [FromQuery] public int Page { get; set; }
    [FromQuery] public int PageSize { get; set; }
    [FromQuery] public string? Order { get; set; }
    [FromQuery] public string? Sort { get; set; }
    [FromQuery] public string? Name { get; set; }
    [FromQuery] public string? Code { get; set; }
    [FromQuery] public bool? EuTradeMember { get; set; }
    [FromQuery] public bool? DevolvedAuthority { get; set; }
}