using KeeperData.Api.Controllers.RequestDtos.Sites;
using KeeperData.Application;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites;
using KeeperData.Core.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    /// <summary>
    /// Operations related to sites (premises/holdings).
    /// </summary>
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "public")]
    [Produces("application/json")]
    [Tags("site")]
    public class SitesController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        /// <summary>
        /// Retrieve a list of available sites
        /// </summary>
        /// <remarks>
        /// Endpoint created to retrieve a list of sites. Parameters can be set to filter by various elements such as Site Identifiers, Site Type, Site Id, Party Id etc. Supports Change Data Capture (CDC) via the lastUpdatedDate parameter.
        /// </remarks>
        /// <param name="request">Query parameters to filter sites.</param>
        /// <response code="200">OK - Successful operation</response>
        /// <response code="400">The request was malformed or could not be processed.</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<SiteDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSites([FromQuery] GetSitesRequest request)
        {
            var typeList = !string.IsNullOrWhiteSpace(request.Type)
                ? request.Type.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList()
                : null;

            var siteIdentifiersList = !string.IsNullOrWhiteSpace(request.SiteIdentifiers)
                ? request.SiteIdentifiers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList()
                : null;

            var siteIdsList = !string.IsNullOrWhiteSpace(request.SiteIds)
                ? request.SiteIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => Guid.TryParse(t, out _))
                    .Select(Guid.Parse)
                    .ToList()
                : null;

            var query = new GetSitesQuery
            {
                SiteIdentifier = request.SiteIdentifier,
                SiteIdentifiers = siteIdentifiersList,
                Type = typeList,
                SiteId = request.SiteId,
                SiteIds = siteIdsList,
                KeeperPartyId = request.KeeperPartyId,
                LastUpdatedDate = request.LastUpdatedDate,
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 10,
                Order = request.Order,
                Sort = request.Sort,
                Cursor = request.Cursor
            };

            var result = await _executor.ExecuteQuery(query);
            return Ok(result);
        }

        /// <summary>
        /// Retrieve a site's detailed information.
        /// </summary>
        /// <remarks>
        /// The endpoint returns an object containing the requested site's full detailed information, including location, identifiers, parties, species, marks, and activities.
        /// </remarks>
        /// <param name="id">The unique identifier (UUID) of the site.</param>
        /// <response code="200">OK - Successful request</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="404">The requested resource was not found.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SiteDocument), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSiteById(string id)
        {
            var result = await _executor.ExecuteQuery(new GetSiteByIdQuery(id));
            return Ok(result);
        }
    }
}