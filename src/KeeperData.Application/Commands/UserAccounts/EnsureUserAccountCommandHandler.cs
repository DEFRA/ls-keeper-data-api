using KeeperData.Application.Services.UserAccounts;
using KeeperData.Core.Documents;
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
            return (account, false);

        var adoptable = await repository.FindByEmailAsync(request.Email, cancellationToken);

        if (adoptable is not null && adoptable.Subject is null)
            return StampSubject(adoptable, request.Subject, now);

        var newAccount = new UserAccountDocument
        {
            Id = Guid.NewGuid().ToString(),
            Subject = request.Subject,
            Email = request.Email,
            CreatedDate = now
        };

        return (newAccount, true);
    }

    private (UserAccountDocument Account, bool Created) StampSubject(UserAccountDocument adoptable, string subject, DateTime now)
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