using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Database.Repositories;

public class CountryRepository : ReferenceDataRepository<CountryListDocument, CountryDocument>, ICountryRepository
{
    public CountryRepository(
        IOptions<MongoConfig> mongoConfig,
        IMongoClient client,
        IUnitOfWork unitOfWork)
        : base(mongoConfig, client, unitOfWork)
    {
    }
}
