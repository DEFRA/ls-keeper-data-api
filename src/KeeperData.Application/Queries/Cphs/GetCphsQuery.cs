using FluentValidation;
using KeeperData.Application.Configuration;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Cphs;

public class GetCphsQuery : IPagedQuery<CphDto>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; }
    public string? Sort { get; set; }
    public string? Cursor { get; set; }
}

public class GetCphsQueryValidator : AbstractValidator<GetCphsQuery>
{
    public GetCphsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort).Must(s => s == "asc" || s == "desc").When(x => !string.IsNullOrEmpty(x.Sort));
    }
}