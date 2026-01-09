using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class FacilityBusinessActivityMapRepository(
    IOptions<MongoConfig> config,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<FacilityBusinessActivityMapListDocument, FacilityBusinessActivityMapDocument>(config, client, unitOfWork), IFacilityBusinessActivityMapRepository
{
    public async Task<FacilityBusinessActivityMapDocument?> FindByActivityCodeAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return null;

        var activityMaps = await GetAllAsync(cancellationToken);

        return activityMaps.FirstOrDefault(s => s.FacilityActivityCode.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));
    }
}