using KeeperData.Core.DTOs;
using KeeperData.Core.Entities;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace KeeperData.Infrastructure.Database.Repositories;

public class CphRepository(ICphSqliteCacheService cacheService) : ICphRepository
{
    private readonly ICphSqliteCacheService _cacheService = cacheService;

    public async Task<(List<CphDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? sort, CancellationToken cancellationToken = default)
    {
        var dbPath = _cacheService.GetCurrentDbPath();
        if (dbPath is null)
            return ([], 0);

        var options = new DbContextOptionsBuilder<CphDbContext>()
            .UseSqlite($"Data Source={dbPath};Mode=ReadOnly")
            .Options;

        await using var dbContext = new CphDbContext(options);

        var query = dbContext.Cphs.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        IQueryable<CphEntity> sorted = string.Equals(sort, "desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(c => c.Cph)
            : query.OrderBy(c => c.Cph);

        var pagedItems = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CphDto { Cph = c.Cph })
            .ToListAsync(cancellationToken);

        return (pagedItems, totalCount);
    }
}
