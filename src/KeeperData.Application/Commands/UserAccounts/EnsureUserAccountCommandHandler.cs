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

        if (account is not null)
        {
            await EnsureEmailIsNotTakenByAnotherAccountAsync(request, account, cancellationToken);
            return (account, false);
        }

        var adoptable = await repository.FindByEmailAsync(request.Email, cancellationToken);

        if (adoptable is not null)
        {
            if (adoptable.Subject is null)
                return AdoptExistingAccount(adoptable, request.Subject);

            throw new ConflictException(
                $"Email '{request.Email}' is already associated with a different account.");
        }

        var newAccount = new UserAccountDocument
        {
            Id = Guid.NewGuid().ToString(),
            Subject = request.Subject,
            Email = request.Email,
            CreatedDate = now
        };

        return (newAccount, true);
    }

    /// <summary>
    /// Guards against overwriting the resolved account's email with one already claimed by a
    /// different subject. This is a permanent business rule violation, not a transient race, so it
    /// is surfaced as a 409 Conflict rather than falling through to a duplicate key retry.
    /// </summary>
    private async Task EnsureEmailIsNotTakenByAnotherAccountAsync(
        EnsureUserAccountCommand request,
        UserAccountDocument account,
        CancellationToken cancellationToken)
    {
        if (string.Equals(account.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            return;

        var emailOwner = await repository.FindByEmailAsync(request.Email, cancellationToken);

        if (emailOwner is not null && emailOwner.Id != account.Id)
        {
            throw new ConflictException(
                $"Email '{request.Email}' is already associated with a different account.");
        }
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