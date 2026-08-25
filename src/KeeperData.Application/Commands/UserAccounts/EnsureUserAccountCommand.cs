using FluentValidation;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Commands.UserAccounts;

/// <summary>
/// Ensures a user account exists for the supplied identity provider claims, refreshes its profile
/// fields and rebuilds its CPH association graph from master data.
/// </summary>
/// <remarks>
/// The account and its association graph live in a single document, so the write is atomic without
/// requiring a multi document transaction.
/// </remarks>
public record EnsureUserAccountCommand(
    string Subject,
    string Email,
    string GivenName,
    string FamilyName) : ICommand<EnsureUserAccountResult>;

public record EnsureUserAccountResult(UserAccountDto Account, bool Created);

public class EnsureUserAccountCommandValidator : AbstractValidator<EnsureUserAccountCommand>
{
    public EnsureUserAccountCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(x => x.GivenName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.FamilyName).NotEmpty().MaximumLength(256);
    }
}
