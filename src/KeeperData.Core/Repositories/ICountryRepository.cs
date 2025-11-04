using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface ICountryRepository : IReferenceDataRepository<CountryListDocument, CountryDocument>
{
}
