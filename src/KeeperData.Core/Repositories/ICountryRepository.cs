using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface ICountryRepository : IReferenceDataRepository<CountryListDocument, CountryDocument>
{
    Task<CountryDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);
    
    Task<(string? countryId, string? countryName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}
