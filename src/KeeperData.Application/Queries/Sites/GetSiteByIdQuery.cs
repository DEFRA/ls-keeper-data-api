using FluentValidation;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Sites;

public record GetSiteByIdQuery(string Id) : IQuery<SiteDto>;

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