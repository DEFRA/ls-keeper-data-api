using KeeperData.Api.Controllers.RequestDtos.Countries;
using KeeperData.Application;
using KeeperData.Application.Queries.Countries;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    /// <summary>
    /// Operations related to countries (site reference data).
    /// </summary>
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "public")]
    [Produces("application/json")]
    [Tags("site reference")]
    public class CountriesController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        /// <summary>
        /// Retrieve a list of ISO Countries
        /// </summary>
        /// <remarks>
        /// Endpoint created to retrieve a list of ISO Countries. Parameters can be set to filter by name, code, EU trade membership, and devolved authority status. Supports Change Data Capture (CDC) via the lastUpdatedDate parameter.
        /// </remarks>
        /// <param name="request">Query parameters to filter countries.</param>
        /// <response code="200">OK - Successful operation</response>
        /// <response code="400">The request was malformed or could not be processed.</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<CountryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCountries([FromQuery] GetCountriesRequest request)
        {
            var codeList = !string.IsNullOrWhiteSpace(request.Code)
                ? request.Code.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList()
                : null;

            var query = new GetCountriesQuery
            {
                Name = request.Name,
                Code = codeList,
                DevolvedAuthority = request.DevolvedAuthority,
                EuTradeMember = request.EuTradeMember,
                LastUpdatedDate = request.LastUpdatedDate,
                Order = request.Order,
                Sort = request.Sort,
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 10
            };
            var result = await _executor.ExecuteQuery(query);
            return Ok(result);
        }

        /// <summary>
        /// Retrieve a country's detailed information.
        /// </summary>
        /// <remarks>
        /// The endpoint returns an object containing the requested country detailed information.
        /// </remarks>
        /// <param name="id">The unique identifier (UUID) of the country.</param>
        /// <response code="200">OK - Successful request</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="404">The requested resource was not found.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CountryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCountryById(string id)
        {
            var result = await _executor.ExecuteQuery(new GetCountryByIdQuery(id));
            return Ok(result);
        }
    }
}