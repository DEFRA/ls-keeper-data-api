using KeeperData.Api.Controllers.RequestDtos.Sites;
using KeeperData.Application;
using KeeperData.Application.Queries.Sites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/[controller]")]
    public class SitesController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        [HttpGet]
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
                Sort = request.Sort
            };

            var result = await _executor.ExecuteQuery(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSiteById(string id)
        {
            var result = await _executor.ExecuteQuery(new GetSiteByIdQuery(id));
            return Ok(result);
        }
    }
}