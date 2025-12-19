using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class RoleRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<RoleListDocument, RoleDocument>(mongoConfig, client, unitOfWork), IRoleRepository
{
    public new async Task<RoleDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var roles = await GetAllAsync(cancellationToken);
        return roles.FirstOrDefault(r => r.IdentifierId?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<(string? roleId, string? roleCode, string? roleName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null, null);

        var roles = await GetAllAsync(cancellationToken);

        // Try exact match on Code first (case-insensitive)
        var role = roles.FirstOrDefault(r => r.Code?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        // If not found by code, try name match
        role ??= roles.FirstOrDefault(r => r.Name?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        return role != null
            ? (role.IdentifierId, role.Code, role.Name)
            : (null, null, null);
    }
}