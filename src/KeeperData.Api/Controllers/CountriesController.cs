using KeeperData.Api.Controllers.RequestDtos.Countries;
using KeeperData.Application;
using KeeperData.Application.Queries.Countries;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        [HttpGet]
        public async Task<IActionResult> GetCountries([FromQuery] GetCountriesRequest request)
        {
            var query = new GetCountriesQuery
            {
                Name = request.Name,
                Code = request.Code,
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