using FluentValidation;
using KeeperData.Application.Configuration;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Parties;

public class GetPartiesQuery : IPagedQuery<PartyDocument>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; }
    public string? Sort { get; set; }
    public string? Cursor { get; set; }
}

public class GetPartiesQueryValidator : AbstractValidator<GetPartiesQuery>
{
    public GetPartiesQueryValidator(QueryValidationConfig<GetPartiesQueryValidator> config)
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, config.MaxPageSize);

        RuleFor(x => x.FirstName).NotEmpty().When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).NotEmpty().When(x => x.LastName is not null);
        RuleFor(x => x.Email).NotEmpty().When(x => x.Email is not null);
        RuleFor(x => x.Sort).Must(s => s == "asc" || s == "desc").When(x => !string.IsNullOrEmpty(x.Sort));
        RuleFor(x => x.Order).NotEmpty().When(x => !string.IsNullOrEmpty(x.Sort));
    }
}