using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceSiteTypes;

public class GetReferenceSiteTypesQuery : IQuery<ReferenceSiteTypeListResponse>
{
    public DateTime? LastUpdatedDate { get; set; }
}