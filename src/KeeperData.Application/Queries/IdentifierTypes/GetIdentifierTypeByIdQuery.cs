using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.IdentifierTypes;

public record GetIdentifierTypeByIdQuery(string Id) : IQuery<IdentifierTypeDTO>;