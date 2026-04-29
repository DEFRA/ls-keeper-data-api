using KeeperData.Api.Controllers.RequestDtos.ReferenceRoles;
using KeeperData.Application;
using KeeperData.Application.Queries.ReferenceRoles;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers;

/// <summary>
/// Roles  reference data
/// </summary>
[Authorize(Policy = "BasicOrBearer")]
[ApiController]
[Route("api/reference/roles")]
[ApiExplorerSettings(GroupName = "public")]
[Produces("application/json")]
[Tags("site reference")]
public class ReferenceRolesController(IRequestExecutor executor) : ControllerBase
{
    private readonly IRequestExecutor _executor = executor;

    /// <summary>
    /// Retrieve all roles
    /// </summary>
    /// <remarks>
    /// Get a list of all available roles
    /// </remarks>
    /// <param name="request">Query parameters to filter roles.</param>
    /// <response code="200">OK - Successful response</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Access token is not set or invalid.</response>
    /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(RoleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetRoles([FromQuery] GetReferenceRolesRequest request)
    {
        var query = new GetReferenceRolesQuery
        {
            LastUpdatedDate = request.LastUpdatedDate
        };

        var result = await _executor.ExecuteQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve a role detailed information based on its identifier.
    /// </summary>
    /// <remarks>
    /// The endpoint returns an object containing the requested role and any relations information.
    /// </remarks>
    /// <param name="id">The unique identifier (UUID) of the role.</param>
    /// <response code="200">OK - Successful request</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Access token is not set or invalid.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="503">Service Unavailable</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetRoleById(string id)
    {
        var result = await _executor.ExecuteQuery(new GetReferenceRoleByIdQuery(id));
        return Ok(result);
    }
}
