using KeeperData.Api.Controllers.RequestDtos.Holdings;
using KeeperData.Application;
using KeeperData.Application.Queries.Holdings;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers;

/// <summary>
/// Operations related to holding details served from the cached SAM read model.
/// </summary>
[Authorize(Policy = "BasicOrBearer")]
[ApiController]
[Route("api/holdings")]
[ApiExplorerSettings(GroupName = "public")]
[Produces("application/json")]
[Tags("holdings")]
public class HoldingsController(IRequestExecutor executor, IReadModelSqliteCacheService readModelCache) : ControllerBase
{
    private readonly IRequestExecutor _executor = executor;
    private readonly IReadModelSqliteCacheService _readModelCache = readModelCache;

    /// <summary>
    /// Retrieve detailed holding information by CPH.
    /// </summary>
    /// <remarks>
    /// Serves holding detail for a CPH from the locally cached SAM read model.
    /// </remarks>
    /// <param name="request">The route parameters containing county, parish, and holding segments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">OK - Holding detail found.</response>
    /// <response code="400">A segment fails its constraint.</response>
    /// <response code="401">Access token is not set or invalid.</response>
    /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
    /// <response code="404">CPH not in the snapshot.</response>
    /// <response code="503">SQLite read model cache is not yet available.</response>
    /// <response code="500">The server encountered an unexpected error.</response>
    [HttpGet("{county}/{parish}/{holding}")]
    [ProducesResponseType(typeof(HoldingDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHoldingDetail(
        [FromRoute] GetHoldingDetailRequest request,
        CancellationToken cancellationToken)
    {
        if (!_readModelCache.IsLoaded)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "The SAM read model is not cached locally, so holding details cannot be resolved.");
        }

        var query = new GetHoldingDetailQuery
        {
            County = request.County,
            Parish = request.Parish,
            Holding = request.Holding
        };

        var result = await _executor.ExecuteQuery(query, cancellationToken);
        return Ok(result);
    }
}