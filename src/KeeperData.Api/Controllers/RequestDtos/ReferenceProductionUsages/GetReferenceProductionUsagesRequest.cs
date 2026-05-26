using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.RequestDtos.ReferenceProductionUsages;

public class GetReferenceProductionUsagesRequest
{
    /// <summary>
    /// Returns only records that have been updated since the provided timestamp.
    /// </summary>
    [FromQuery] public DateTime? LastUpdatedDate { get; set; }
}