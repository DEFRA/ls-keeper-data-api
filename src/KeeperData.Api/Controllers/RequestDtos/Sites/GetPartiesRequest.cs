using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.Sites;

public class GetPartiesRequest
{
    [FromQuery] public string? FirstName { get; set; }
    [FromQuery] public string? LastName { get; set; }
    [FromQuery] public string? Email { get; set; }
    [FromQuery] public DateTime? LastUpdatedDate { get; set; }
    [FromQuery] public int? Page { get; set; }
    [FromQuery] public int? PageSize { get; set; }
    [FromQuery] public string? Order { get; set; }
    [FromQuery] public string? Sort { get; set; }
}