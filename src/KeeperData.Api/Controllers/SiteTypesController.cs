using KeeperData.Application;
using KeeperData.Application.Queries.SiteTypes;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers;

/// <summary>
/// Operations related to site types and their associated activities.
/// </summary>
[Authorize(Policy = "BasicOrBearer")]
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "public")]
[Produces("application/json")]
[Tags("site reference")]
public class SiteTypesController(IRequestExecutor executor) : ControllerBase
{
    private readonly IRequestExecutor _executor = executor;

    /// <summary>
    /// Retrieve all Site Types with their associated Site Activities.
    /// </summary>
    /// <remarks>
    /// Returns all Site Types. Each Site Type includes zero, one, or many Site Activities.
    /// Site Types are uniquely identified by SiteTypeCode.
    /// Site Activities are uniquely identified by SiteActivityCode within a SiteTypeCode.
    /// </remarks>
    /// <response code="200">OK - Successful operation</response>
    /// <response code="401">Access token is not set or invalid.</response>
    /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
    /// <response code="500">The server encountered an unexpected error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<SiteTypeDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSiteTypes()
    {
        var result = await _executor.ExecuteQuery(new GetSiteTypesQuery());
        return Ok(result);
    }
}