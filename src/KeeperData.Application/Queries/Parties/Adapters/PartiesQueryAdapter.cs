using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties.Builders;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Parties.Adapters;

public class PartiesQueryAdapter(IPartiesRepository repository)
{
    public async Task<(List<PartyDto> Items, int TotalCount, string? NextCursor)> GetPartiesAsync(
        GetPartiesQuery query,
        CancellationToken cancellationToken = default)
    {
        var options = new CursorPaginationHelper.PagedQueryOptions<PartyDocument, PartiesQueryInternal>
        {
            Query = new PartiesQueryInternal(query),
            BaseFilter = PartyFilterBuilder.Build(query),
            SortDefinition = PartySortBuilder.Build(query),
            SortFieldPath = PartySortBuilder.GetSortFieldPath(query.Order),
            CountAsync = repository.CountAsync,
            FindAsync = repository.FindAsync,
            GetSortValue = doc => GetSortValue(doc, query.Order)
        };

        var (documents, totalCount, nextCursor) = await CursorPaginationHelper.ExecutePagedQueryAsync(options, cancellationToken);

        var dtos = documents.Select(doc => doc.ToDto()).ToList();

        return (dtos, totalCount, nextCursor);
    }

    private static string GetSortValue(PartyDocument doc, string? sortField)
    {
        return (sortField?.ToLowerInvariant()) switch
        {
            "id" => doc.Id,
            "name" => doc.Name ?? string.Empty,
            _ => doc.Name ?? string.Empty
        };
    }

    /// <summary>
    /// Internal wrapper that satisfies IPagedQuery&lt;PartyDocument&gt; for the CursorPaginationHelper,
    /// while the public GetPartiesQuery uses IPagedQuery&lt;PartyDto&gt;.
    /// </summary>
    private class PartiesQueryInternal(GetPartiesQuery query) : IPagedQuery<PartyDocument>
    {
        public int Page => query.Page;
        public int PageSize => query.PageSize;
        public string? Order => query.Order;
        public string? Sort => query.Sort;
        public string? Cursor => query.Cursor;
    }
}