using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Sites;

public class GetSiteByIdQueryHandler(IGenericRepository<SiteDocument> repository) : IQueryHandler<GetSiteByIdQuery, SiteDocument>
{
    private readonly IGenericRepository<SiteDocument> _repository = repository;

    public async Task<SiteDocument> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Document with Id {request.Id} not found.");

        return document;
    }
}