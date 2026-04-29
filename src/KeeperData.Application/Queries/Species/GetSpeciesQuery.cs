using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Species;

public class GetSpeciesQuery : IQuery<IEnumerable<SpeciesListResponse>>
{
    public DateTime? LastUpdatedDate { get; set; }
}