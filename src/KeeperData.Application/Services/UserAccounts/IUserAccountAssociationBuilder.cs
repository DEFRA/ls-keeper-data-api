using KeeperData.Core.Documents;

namespace KeeperData.Application.Services.UserAccounts;

/// <summary>
/// Builds a user's CPH association graph from SAM mastered party data.
/// </summary>
public interface IUserAccountAssociationBuilder
{
    Task<List<CphAssociationDocument>> BuildForEmailAsync(string email, CancellationToken cancellationToken = default);
}