using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.ReferenceSiteTypes;

public class GetReferenceSiteTypeByIdQueryHandler(IReferenceDataCache cache) : IQueryHandler<GetReferenceSiteTypeByIdQuery, ReferenceSiteTypeDto>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<ReferenceSiteTypeDto> Handle(GetReferenceSiteTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var item = _cache.SiteTypes.FirstOrDefault(p => p.IdentifierId == request.Id)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        var dto = new ReferenceSiteTypeDto
        {
            Id = item.IdentifierId,
            Code = item.Code,
            Name = item.Name
        };

        return Task.FromResult(dto);
    }
}