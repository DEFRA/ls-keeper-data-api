using KeeperData.Api.Controllers.RequestDtos.IdentifierTypes;
using KeeperData.Application;
using KeeperData.Application.Queries.IdentifierTypes;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers;

/// <summary>
/// Identifier types endpoint.
/// </summary>
[Authorize(Policy = "BasicOrBearer")]
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "public")]
[Produces("application/json")]
[Tags("site reference")]
public class IdentifierTypesController(IRequestExecutor executor) : ControllerBase
{
    private readonly IRequestExecutor _executor = executor;

    /// <summary>
    /// Retrieve all identifier types
    /// </summary>
    /// <remarks>
    /// Get a list of all available identifier types
    /// </remarks>
    /// <param name="request">Query parameters to filter identifier types.</param>
    /// <response code="200">OK - Successful response</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Access token is not set or invalid.</response>
    /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
    /// <response code="500">Internal server error</response>
    /// <response code="503">Service Unavailable</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IdentifierTypeListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetIdentifierTypes([FromQuery] GetIdentifierTypesRequest request)
    {
        var query = new GetIdentifierTypesQuery
        {
            LastUpdatedDate = request.LastUpdatedDate
        };

        var result = await _executor.ExecuteQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve an identifier type's detailed information.
    /// </summary>
    /// <remarks>
    /// The endpoint returns an object containing the requested identifier type.
    /// </remarks>
    /// <param name="id">The unique identifier (UUID) of the identifier type.</param>
    /// <response code="200">OK - Successful request</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Access token is not set or invalid.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="503">Service Unavailable</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IdentifierTypeDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetIdentifierTypeById(string id)
    {
        var result = await _executor.ExecuteQuery(new GetIdentifierTypeByIdQuery(id));
        return Ok(result);
    }
}