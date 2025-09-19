using FluentValidation;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Sites;

public record GetSiteByIdQuery(string Id) : IQuery<SiteDocument>;

/// <summary>
/// Example implementation only. To remove in future stories.
/// </summary>
public class GetSiteByIdQueryValidator : AbstractValidator<GetSiteByIdQuery>
{
    public GetSiteByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}