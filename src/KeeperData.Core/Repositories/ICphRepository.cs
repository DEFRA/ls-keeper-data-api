using KeeperData.Core.DTOs;

namespace KeeperData.Core.Repositories;

public interface ICphRepository
{
    Task<(List<CphDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? sort, CancellationToken cancellationToken = default);
}
