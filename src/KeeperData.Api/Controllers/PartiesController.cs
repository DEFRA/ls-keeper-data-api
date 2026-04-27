using KeeperData.Api.Controllers.RequestDtos.Parties;
using KeeperData.Application;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    /// <summary>
    /// Operations related to parties (keepers, organisations).
    /// </summary>
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "public")]
    [Produces("application/json")]
    [Tags("party")]
    public class PartiesController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        /// <summary>
        /// Retrieve a list of parties.
        /// </summary>
        /// <remarks>
        /// The endpoint allows searching and retrieving a list of parties. Supports filtering by name, email, and Change Data Capture (CDC) via the lastUpdatedDate parameter.
        /// </remarks>
        /// <param name="request">Query parameters to filter parties.</param>
        /// <response code="200">OK - Successful</response>
        /// <response code="400">The request was malformed or could not be processed.</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<PartyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetParties([FromQuery] GetPartiesRequest request)
        {
            var query = new GetPartiesQuery
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
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
        /// Retrieve details of a party.
        /// </summary>
        /// <remarks>
        /// The endpoint allows to retrieve details of a given party, including their communication details, correspondence address, and roles.
        /// </remarks>
        /// <param name="id">The unique identifier (UUID) of the party.</param>
        /// <response code="200">OK - Successful</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="404">The requested resource was not found.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PartyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartyById(string id)
        {
            var result = await _executor.ExecuteQuery(new GetPartyByIdQuery(id));
            return Ok(result);
        }
    }
}