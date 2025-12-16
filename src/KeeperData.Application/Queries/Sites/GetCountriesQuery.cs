using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Sites;

public class GetCountriesQuery : IPagedQuery<CountrySummaryDocument>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; }
    public string? Sort { get; set; }
    public string? Cursor { get; set; }
}