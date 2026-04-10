using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.IdentifierTypes;

public class GetIdentifierTypesQuery : IQuery<IEnumerable<IdentifierTypeListResponse>>
{
    public DateTime? LastUpdatedDate { get; set; }
}