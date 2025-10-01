using FluentValidation;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Sites;

public class GetSitesQuery : IPagedQuery<SiteDocument>
{
    public string? SiteIdentifier { get; set; }
    public List<string>? Type { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? KeeperPartyId { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; }
    public string? Sort { get; set; }
    public string? Cursor { get; set; }
}

public class GetSitesQueryValidator : AbstractValidator<GetSitesQuery>
{
    public GetSitesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleForEach(x => x.Type).NotEmpty().When(x => x.Type is not null);
        RuleFor(x => x.Sort).Must(s => s == "asc" || s == "desc").When(x => !string.IsNullOrEmpty(x.Sort));
        RuleFor(x => x.Order).NotEmpty().When(x => !string.IsNullOrEmpty(x.Sort));
    }
}