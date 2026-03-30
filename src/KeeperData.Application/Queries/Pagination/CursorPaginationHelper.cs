using KeeperData.Core.Repositories;
using MongoDB.Driver;

namespace KeeperData.Application.Queries.Pagination;

public static class CursorPaginationHelper
{
    public static SortDefinition<T> BuildSortDefinition<T>(string? sortDirection, string sortFieldPath) where T : IEntity
    {
        var sortBuilder = Builders<T>.Sort;
        var direction = sortDirection?.ToLowerInvariant() ?? "asc";

        var primarySort = direction == "desc"
            ? sortBuilder.Descending(sortFieldPath)
            : sortBuilder.Ascending(sortFieldPath);

        if (sortFieldPath == "_id") return primarySort;

        return direction == "desc"
            ? primarySort.Descending(x => x.Id)
            : primarySort.Ascending(x => x.Id);
    }
    public static FilterDefinition<T>? BuildCursorFilter<T>(string? cursor, string? sortDirection, string sortFieldPath) where T : IEntity
    {
        var decoded = CursorHelper.Decode(cursor);
        if (decoded == null) return null;

        var (sortVal, lastId) = decoded.Value;
        var isAscending = (sortDirection?.ToLowerInvariant() ?? "asc") == "asc";

        var builder = Builders<T>.Filter;

        if (isAscending)
        {
            return builder.Or(
                builder.Gt(sortFieldPath, sortVal),
                builder.And(builder.Eq(sortFieldPath, sortVal), builder.Gt(x => x.Id, lastId))
            );
        }
        else
        {
            return builder.Or(
                builder.Lt(sortFieldPath, sortVal),
                builder.And(builder.Eq(sortFieldPath, sortVal), builder.Lt(x => x.Id, lastId))
            );
        }
    }

    public static (FilterDefinition<T> Filter, bool HasValidCursor) ApplyCursorFilter<T>(
        FilterDefinition<T> currentFilter,
        string? cursor,
        string? sortDirection,
        string sortFieldPath) where T : IEntity
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return (currentFilter, false);

        var cursorFilter = BuildCursorFilter<T>(cursor, sortDirection, sortFieldPath);
        if (cursorFilter != null)
        {
            return (Builders<T>.Filter.And(currentFilter, cursorFilter), true);
        }

        return (currentFilter, false);
    }

    public static string? GetNextCursor<T>(List<T>? items, int pageSize, Func<T, string> getSortValueFunc) where T : IEntity
    {
        if (items != null && items.Count == pageSize)
        {
            var lastItem = items.Last();
            var sortVal = getSortValueFunc(lastItem);
            return CursorHelper.Encode(sortVal, lastItem.Id ?? string.Empty);
        }
        return null;
    }
}