using KeeperData.Api.Controllers.RequestDtos.Sites;
using KeeperData.Application;
using KeeperData.Application.Queries.Parties;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartyController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        [HttpGet]
        public async Task<IActionResult> GetParties([FromQuery] GetPartiesRequest request)
        {
            var query = new GetPartiesQuery
            {
                /*  TODO  
                 SiteIdentifier = request.SiteIdentifier,
                    Type = request.Type,
                    SiteId = request.SiteId,
                    KeeperPartyId = request.KeeperPartyId,
                    LastUpdatedDate = request.LastUpdatedDate,
                    Page = request.Page ?? 1,
                    PageSize = request.PageSize ?? 10,
                    Order = request.Order,
                    Sort = request.Sort*/
            };

            var result = await _executor.ExecuteQuery(query);
            return Ok(result);
        }

        /*
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPartyById(string id)
        { TODO
            var result = await _executor.ExecuteQuery(new GetPartyByIdQuery(id));
            return Ok(result);
        }*/
    }
}