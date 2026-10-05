using FluentValidation;
using KeeperData.Core.DTOs;
using System.Text.RegularExpressions;

namespace KeeperData.Application.Queries.Holdings;

public class GetHoldingsQuery : IPagedQuery<HoldingDetail>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; } = "cph";
    public string? Sort { get; set; } = "asc";
    public string? Cursor { get; set; }
    public string? Search { get; set; }
}

public partial class GetHoldingsQueryValidator : AbstractValidator<GetHoldingsQuery>
{
    public GetHoldingsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200)
            .Matches(@"^[\p{L}\p{N}\s/'@.+(),&-]*$")
            .Must(search => search is null || string.IsNullOrWhiteSpace(search) || LetterOrDigitRegex().IsMatch(search))
            .WithMessage("Search must contain at least one letter or digit.")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));
        RuleFor(x => x.Sort)
            .Must(s => string.Equals(s, "asc", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "desc", StringComparison.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.Sort));
        RuleFor(x => x.Order)
            .Must(order => order is not null && new[] { "cph", "identifier", "name", "holdingType", "startDate", "endDate" }
                .Contains(order, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrEmpty(x.Order));
    }

    [GeneratedRegex(@"[\p{L}\p{N}]")]
    private static partial Regex LetterOrDigitRegex();
}