using KeeperData.Application.Queries;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Species;

public record GetSpeciesByIdQuery(string Id) : IQuery<SpeciesDTO>;