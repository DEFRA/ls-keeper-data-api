using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.ReferenceSiteTypes;

public record GetReferenceSiteTypeByIdQuery(string Id) : IQuery<ReferenceSiteTypeDto>;