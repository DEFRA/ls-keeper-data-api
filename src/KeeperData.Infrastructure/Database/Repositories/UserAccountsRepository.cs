using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class UserAccountsRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : GenericRepository<UserAccountDocument>(
        mongoConfig,
        client,
        unitOfWork), IUserAccountsRepository
{
    public async Task<UserAccountDocument?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserAccountDocument>.Filter.And(
            Builders<UserAccountDocument>.Filter.Eq(x => x.Subject, subject),
            Builders<UserAccountDocument>.Filter.Eq(x => x.Deleted, false));

        return await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserAccountDocument?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserAccountDocument>.Filter.And(
            Builders<UserAccountDocument>.Filter.Eq(x => x.Email, email),
            Builders<UserAccountDocument>.Filter.Eq(x => x.Deleted, false));

        return await _collection
            .Find(filter, new FindOptions { Collation = IndexDefaults.CollationCaseInsensitive })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
