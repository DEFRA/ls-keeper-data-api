using KeeperData.Api.Controllers.RequestDtos.CphAssociations;
using KeeperData.Api.Controllers.ResponseDtos.CphAssociations;
using KeeperData.Application;
using KeeperData.Application.Queries.CphAssociations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers;

/// <summary>
/// Operations related to user-role-CPH associations.
/// </summary>
[Authorize(Policy = "BasicOrBearer")]
[ApiController]
[Route("cph-associations")]
[ApiExplorerSettings(GroupName = "public")]
[Produces("application/json")]
[Tags("user-accounts")]
public class CphAssociationsController(IRequestExecutor executor) : ControllerBase
{
    private readonly IRequestExecutor _executor = executor;

    /// <summary>
    /// Retrieve a list of CPH associations by email.
    /// </summary>
    /// <remarks>
    /// Asks KRDS which CPHs an email address is associated with, and in what role.
    /// </remarks>
    /// <param name="request">The request containing the email to query.</param>
    /// <param name="readModelCache">The SQLite cache service for the read model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">OK - Array of association objects, deduplicated.</response>
    /// <response code="400">Missing/blank/malformed email, or business error.</response>
    /// <response code="401">Access token is not set or invalid.</response>
    /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
    /// <response code="500">The server encountered an unexpected error</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CphAssociationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCphAssociations(
        [FromQuery] GetCphAssociationsRequest request,
        [FromServices] KeeperData.Core.Services.IReadModelSqliteCacheService readModelCache,
        CancellationToken cancellationToken)
    {
        if (!readModelCache.IsLoaded)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "The SAM read model is not cached locally, so CPH associations cannot be resolved.");
        }

        var query = new GetCphAssociationsQuery
        {
            Email = request.Email!
        };

        var result = await _executor.ExecuteQuery(query, cancellationToken);
        
        var response = result.Select(r => new CphAssociationResponse
        {
            Cph = r.Cph,
            Role = r.Role
        }).ToList();

        return Ok(response);
    }
}
