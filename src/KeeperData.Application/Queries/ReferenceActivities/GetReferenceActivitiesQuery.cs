using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceActivities;

public class GetReferenceActivitiesQuery : IQuery<ReferenceActivityListResponse>
{
    public DateTime? LastUpdatedDate { get; set; }
}