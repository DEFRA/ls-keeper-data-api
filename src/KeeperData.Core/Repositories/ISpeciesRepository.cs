using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface ISpeciesRepository : IReferenceDataRepository<SpeciesListDocument, SpeciesDocument>
{
    new Task<SpeciesDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);

    Task<(string? speciesId, string? speciesName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}