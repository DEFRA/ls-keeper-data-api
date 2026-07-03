using KeeperData.Api.Controllers.RequestDtos.Cphs;
using KeeperData.Application;
using KeeperData.Application.Queries.Cphs;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    /// <summary>
    /// Operations related to CPH (County Parish Holding) data served from the cached SQLite export.
    /// </summary>
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/v2/cphs")]
    [ApiExplorerSettings(GroupName = "public")]
    [Produces("application/json")]
    [Tags("cph")]
    public class CphsController(IRequestExecutor executor, ICphSqliteCacheService cphCache) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        /// <summary>
        /// Retrieve a paginated list of CPH records from the cached SQLite export.
        /// </summary>
        /// <remarks>
        /// Returns CPH data from the locally cached SQLite file (downloaded from S3).
        /// Returns 503 if the cache has not yet loaded.
        /// </remarks>
        /// <param name="request">Query parameters for pagination and sorting.</param>
        /// <response code="200">OK - Paginated list of CPH records</response>
        /// <response code="400">The request was malformed or could not be processed.</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="503">SQLite cache is not yet available. Data is still loading.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<CphDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCphs([FromQuery] GetCphsRequest request)
        {
            if (!cphCache.IsLoaded)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "CPH data is not yet available. The SQLite cache is still loading."
                });
            }

            var query = new GetCphsQuery
            {
                Page = request.Page ?? 1,
                PageSize = Math.Clamp(request.PageSize ?? 10, 1, 100),
                Order = request.Order,
                Sort = request.Sort,
                Cursor = request.Cursor
            };

            var result = await _executor.ExecuteQuery(query);

            if (cphCache.DataTimestamp.HasValue)
            {
                Response.Headers["X-Data-Timestamp"] = cphCache.DataTimestamp.Value.ToString("o");
            }

            return Ok(result);
        }
    }
}
