using Asp.Versioning;
using KeeperData.Application;
using KeeperData.Application.Queries.Sites;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class SiteController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _executor.ExecuteQuery(new GetSiteByIdQuery(id));
            return Ok(result);
        }
    }
}