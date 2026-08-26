using FluentValidation;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.UserAccounts;

/// <summary>
/// Read only lookup of a user account by identity provider subject. Does not refresh the account
/// or its CPH associations.
/// </summary>
public record GetUserAccountBySubjectQuery(string Subject) : IQuery<UserAccountDto>;

public class GetUserAccountBySubjectQueryValidator : AbstractValidator<GetUserAccountBySubjectQuery>
{
    public GetUserAccountBySubjectQueryValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(256);
    }
}