using KeeperData.Core.Documents;

namespace KeeperData.Core.Repositories;

public interface IUserAccountsRepository : IGenericRepository<UserAccountDocument>
{
    Task<UserAccountDocument?> FindBySubjectAsync(string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an account by email address, case insensitively.
    /// </summary>
    Task<UserAccountDocument?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
