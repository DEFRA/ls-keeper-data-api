using KeeperData.Application;
using KeeperData.Application.Queries.Sites;
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
        /*
            [HttpGet("{id}")]
            public async Task<IActionResult> GetSiteById(string id)
            {
            //      var result = await _executor.ExecuteQuery(new GetSiteByIdQuery(id));
            //       return Ok(result);
        }*/
    }
}