using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceProductionUsages;

public record GetReferenceProductionUsageByIdQuery(string Id) : IQuery<ReferenceProductionUsageDto>;