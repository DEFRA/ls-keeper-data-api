using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Sites;

public class GetCountriesQuery : IPagedQuery<CountryDTO>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; }
    public string? Sort { get; set; }
    public string? Cursor { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public bool? EuTradeMember { get; set; }
    public bool? DevolvedAuthority { get; set; }
}