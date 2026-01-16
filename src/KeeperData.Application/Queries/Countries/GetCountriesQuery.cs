using FluentValidation;
using KeeperData.Application.Configuration;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Countries;

public class GetCountriesQuery : IPagedQuery<CountryDTO>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Order { get; set; }
    public string? Sort { get; set; }
    public string? Cursor { get; set; }
    public string? Name { get; set; }
    public List<string>? Code { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public bool? EuTradeMember { get; set; }
    public bool? DevolvedAuthority { get; set; }
}

public class GetCountriesQueryValidator : AbstractValidator<GetCountriesQuery>
{
    public GetCountriesQueryValidator(QueryValidationConfig<GetCountriesQueryValidator> config)
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, config.MaxPageSize);
        RuleForEach(x => x.Code).NotEmpty().When(x => x.Code is not null);
        RuleFor(x => x.Code).Must(x => x == null || x.Count <= config.MaxQueryableTypes).WithMessage($"Code count must be between 0 and {config.MaxQueryableTypes}");
        RuleFor(x => x.Sort).Must(s => s == "asc" || s == "desc").When(x => !string.IsNullOrEmpty(x.Sort));
        RuleFor(x => x.Order).NotEmpty().When(x => !string.IsNullOrEmpty(x.Sort));
    }
}