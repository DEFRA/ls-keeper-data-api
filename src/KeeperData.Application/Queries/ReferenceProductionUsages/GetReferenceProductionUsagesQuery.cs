using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceProductionUsages;

public class GetReferenceProductionUsagesQuery : IQuery<ReferenceProductionUsageListResponse>
{
    public DateTime? LastUpdatedDate { get; set; }
}