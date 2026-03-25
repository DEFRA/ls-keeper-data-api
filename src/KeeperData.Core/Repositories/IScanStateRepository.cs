using KeeperData.Core.Documents;

namespace KeeperData.Core.Repositories;

public interface IScanStateRepository
{
    Task<ScanStateDocument?> GetByIdAsync(string scanSourceId, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScanStateDocument scanState, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScanStateDocument>> GetAllAsync(int skip, int limit, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}