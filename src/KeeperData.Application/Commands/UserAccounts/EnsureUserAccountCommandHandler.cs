using KeeperData.Application.Services.UserAccounts;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Commands.UserAccounts;

public class EnsureUserAccountCommandHandler(
    IUserAccountsRepository repository,
    IUserAccountAssociationBuilder associationBuilder,
    ILogger<EnsureUserAccountCommandHandler> logger)
    : ICommandHandler<EnsureUserAccountCommand, EnsureUserAccountResult>
{
    public async Task<EnsureUserAccountResult> Handle(EnsureUserAccountCommand request, CancellationToken cancellationToken)
    {
        var associations = await associationBuilder.BuildForEmailAsync(request.Email, cancellationToken);

        try
        {
            return await EnsureAsync(request, associations, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            logger.LogInformation(ex,
                "Concurrent ensure detected for user account, resolving against the existing account.");

            return await EnsureAsync(request, associations, cancellationToken);
        }
    }

    private async Task<EnsureUserAccountResult> EnsureAsync(
        EnsureUserAccountCommand request,
        List<CphAssociationDocument> associations,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var (account, created) = await ResolveAccountAsync(request, now, cancellationToken);

        OverwriteProfile(account, request, associations, now);

        await PersistAsync(account, created, cancellationToken);

        return new EnsureUserAccountResult(account.ToDto(), created);
    }

    private async Task<(UserAccountDocument Account, bool Created)> ResolveAccountAsync(
        EnsureUserAccountCommand request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var account = await repository.FindBySubjectAsync(request.Subject, cancellationToken);
        var emailMatch = await repository.FindByEmailAsync(request.Email, cancellationToken);

        // An email may be associated with a single identity. If the email match is already bound to
        // a different subject, binding it here would split the keeper across two accounts.
        if (emailMatch?.Subject is not null && emailMatch.Subject != request.Subject)
            throw new ConflictException("The email address is already associated with a different user account.");

        if (account is not null)
            return (account, false);

        // Only adopt an account that has no subject bound yet; the subject is stamped once and never
        // overwritten.
        if (emailMatch is not null)
            return AdoptExistingAccount(emailMatch, request.Subject);

        var newAccount = new UserAccountDocument
        {
            Id = Guid.NewGuid().ToString(),
            Subject = request.Subject,
            Email = request.Email,
            CreatedDate = now
        };

        return (newAccount, true);
    }

    private static (UserAccountDocument Account, bool Created) AdoptExistingAccount(UserAccountDocument adoptable, string subject)
    {
        adoptable.Subject = subject;
        return (adoptable, false);
    }

    private static void OverwriteProfile(
        UserAccountDocument account,
        EnsureUserAccountCommand request,
        List<CphAssociationDocument> associations,
        DateTime now)
    {
        account.Email = request.Email;
        account.FirstName = request.GivenName;
        account.LastName = request.FamilyName;
        account.DisplayName = BuildDisplayName(request.GivenName, request.FamilyName);
        account.CphAssociations = associations;
        account.AssociationsRefreshedDate = now;
        account.LastUpdatedDate = now;
    }

    private async Task PersistAsync(UserAccountDocument account, bool created, CancellationToken cancellationToken)
    {
        if (created)
            await repository.AddAsync(account, cancellationToken);
        else
            await repository.UpdateAsync(account, cancellationToken);
    }

    private static string BuildDisplayName(string givenName, string familyName) =>
        string.Join(' ', new[] { givenName, familyName }.Where(n => !string.IsNullOrWhiteSpace(n))).Trim();
}