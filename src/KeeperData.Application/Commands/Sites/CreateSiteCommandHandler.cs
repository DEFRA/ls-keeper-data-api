using KeeperData.Core.Documents;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Commands.Sites;

/// <summary>
/// Example implementation only. To remove in future stories.
/// </summary>
/// <param name="repository"></param>
public class CreateSiteCommandHandler(IGenericRepository<SiteDocument> repository)
    : ITrackedCommandHandler<CreateSiteCommand, string>
{
    private readonly IGenericRepository<SiteDocument> _repository = repository;

    public async Task<TrackedResult<string>> Handle(CreateSiteCommand request, CancellationToken cancellationToken)
    {
        var site = Site.Create(1, "Holding", request.Name, "England");
        site.AddSiteIdentifier(DateTime.UtcNow, Guid.NewGuid().ToString(), "CPH");

        var document = SiteDocument.FromDomain(site);
        await _repository.AddAsync(document, cancellationToken);

        return new TrackedResult<string>(site.Id, site);
    }
}