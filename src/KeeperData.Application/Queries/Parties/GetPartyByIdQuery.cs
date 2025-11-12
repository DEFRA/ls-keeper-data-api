using FluentValidation;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Parties;

public record GetPartyByIdQuery(string Id) : IQuery<PartyDocument>;

/// <summary>
/// Example implementation only. To remove in future stories.
/// </summary>
public class GetPartyByIdQueryValidator : AbstractValidator<GetPartyByIdQuery>
{
    public GetPartyByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}