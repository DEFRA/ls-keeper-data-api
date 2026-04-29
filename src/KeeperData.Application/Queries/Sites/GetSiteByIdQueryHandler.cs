using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Sites;

public class GetSiteByIdQueryHandler(IGenericRepository<SiteDocument> repository) : IQueryHandler<GetSiteByIdQuery, SiteDto>
{
    public async Task<SiteDto> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await repository.GetByIdAsync(request.Id, cancellationToken)
                       ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        return document.ToDto();
    }
}