using KeeperData.Core.Documents;

namespace KeeperData.Core.Repositories;

public interface ISiteTypeMapRepository
{
    Task<SiteTypeMapDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);

    Task<SiteTypeMapDocument?> FindByTypeCodeAsync(string? typeCode, CancellationToken cancellationToken = default);
}