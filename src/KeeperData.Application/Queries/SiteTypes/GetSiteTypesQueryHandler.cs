using KeeperData.Core.DTOs;
using KeeperData.Core.Services;

namespace KeeperData.Application.Queries.SiteTypes;

public class GetSiteTypesQueryHandler(IReferenceDataCache cache)
    : IQueryHandler<GetSiteTypesQuery, List<SiteTypeDTO>>
{
    private readonly IReferenceDataCache _cache = cache;

    public Task<List<SiteTypeDTO>> Handle(GetSiteTypesQuery request, CancellationToken cancellationToken)
    {
        var result = _cache.SiteTypeMaps
            .Select(map => new SiteTypeDTO
            {
                Id = map.IdentifierId,
                Type = new SiteTypeInfoDTO
                {
                    Code = map.Type.Code,
                    Name = map.Type.Name
                },
                Activities = map.Activities
                    .Select(a => new SiteActivityInfoDTO
                    {
                        Code = a.Code,
                        Name = a.Name
                    })
                    .ToList()
            })
            .ToList();

        return Task.FromResult(result);
    }
}