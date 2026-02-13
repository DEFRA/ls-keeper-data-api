using KeeperData.Api.Controllers.RequestDtos.Countries;
using KeeperData.Application;
using KeeperData.Application.Queries.Countries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "public")]
    public class CountriesController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        [HttpGet]
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCountryById(string id)
        {
            var result = await _executor.ExecuteQuery(new GetCountryByIdQuery(id));
            return Ok(result);
        }
    }
}