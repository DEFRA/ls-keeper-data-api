using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceActivities;

public record GetReferenceActivityByIdQuery(string Id) : IQuery<ReferenceActivityDto>;