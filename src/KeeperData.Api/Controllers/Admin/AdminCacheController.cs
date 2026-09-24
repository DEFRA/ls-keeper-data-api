using KeeperData.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/sqlite-cache")]
[ApiExplorerSettings(GroupName = "internal")]
[Authorize(Policy = "BasicOrBearer")]
[Authorize(Policy = "AdminScopeOrApiKey")]
[Tags("admin")]
public sealed class AdminCacheController(
    ICphSqliteCacheService cphCache,
    IReadModelSqliteCacheService readModelCache,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(CacheRefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Refresh(
        [FromQuery] string cache = "all",
        [FromQuery] bool force = true,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("AdminEndpointsEnabled"))
            return NotFound();

        if (cache is not ("all" or "cph" or "read-model"))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid cache", Detail = "Use all, cph, or read-model." });

        CacheRefreshResult[] results = cache switch
        {
            "cph" => [await cphCache.ForceRefreshAsync(force, cancellationToken)],
            "read-model" => [await readModelCache.ForceRefreshAsync(force, cancellationToken)],
            _ => await Task.WhenAll(
                cphCache.ForceRefreshAsync(force, cancellationToken),
                readModelCache.ForceRefreshAsync(force, cancellationToken))
        };

        var failed = results.Where(result => result.Status == "Failed").ToArray();
        if (failed.Length > 0)
            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "SQLite cache refresh failed",
                Detail = string.Join("; ", failed.Select(result => $"{result.Cache}: {result.Error}"))
            });

        return Ok(new CacheRefreshResponse(DateTime.UtcNow, results));
    }
}

public sealed record CacheRefreshResponse(DateTime Timestamp, IReadOnlyList<CacheRefreshResult> Results);