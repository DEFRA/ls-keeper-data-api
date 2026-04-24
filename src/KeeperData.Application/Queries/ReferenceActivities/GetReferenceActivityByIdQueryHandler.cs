using KeeperData.Application.Queries;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.ReferenceActivities;

public class GetReferenceActivityByIdQueryHandler(IReferenceDataCache cache) : IQueryHandler<GetReferenceActivityByIdQuery, ReferenceActivityDto>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<ReferenceActivityDto> Handle(GetReferenceActivityByIdQuery request, CancellationToken cancellationToken)
    {
        var item = _cache.SiteActivityTypes.FirstOrDefault(a => a.IdentifierId == request.Id)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        var dto = new ReferenceActivityDto
        {
            Id = item.IdentifierId,
            Code = item.Code,
            Name = item.Name
        };

        return Task.FromResult(dto);
    }
}