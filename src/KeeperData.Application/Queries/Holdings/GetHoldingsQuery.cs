using FluentValidation;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Holdings;

public class GetHoldingsQuery : IPagedQuery<HoldingDetail>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; } = "cph";
    public string? Sort { get; set; } = "asc";
    public string? Cursor { get; set; }
}

public class GetHoldingsQueryValidator : AbstractValidator<GetHoldingsQuery>
{
    public GetHoldingsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Sort)
            .Must(s => string.Equals(s, "asc", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "desc", StringComparison.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.Sort));
        RuleFor(x => x.Order)
            .Must(order => order is not null && new[] { "cph", "identifier", "name", "holdingType", "startDate", "endDate" }
                .Contains(order, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.Order));
    }
}