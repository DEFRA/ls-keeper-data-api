using KeeperData.Application.Queries;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.ReferenceProductionUsages;

public class GetReferenceProductionUsageByIdQueryHandler(IReferenceDataCache cache) : IQueryHandler<GetReferenceProductionUsageByIdQuery, ReferenceProductionUsageDto>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<ReferenceProductionUsageDto> Handle(GetReferenceProductionUsageByIdQuery request, CancellationToken cancellationToken)
    {
        var item = _cache.ProductionUsages.FirstOrDefault(p => p.IdentifierId == request.Id)
            ?? throw new NotFoundException($"Document with Id {request.Id} not found.");

        var dto = new ReferenceProductionUsageDto
        {
            Id = item.IdentifierId,
            Code = item.Code,
            Description = item.Description
        };

        return Task.FromResult(dto);
    }
}