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
        public async Task<IActionResult> GetCountries([FromQuery] GetCountriesQuery query)
        {
            var result = await _executor.ExecuteQuery(query);
            return Ok(result);
        }
    }
}