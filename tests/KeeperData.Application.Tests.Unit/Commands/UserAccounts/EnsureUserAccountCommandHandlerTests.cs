using FluentAssertions;
using KeeperData.Application.Commands.UserAccounts;
using KeeperData.Application.Services.UserAccounts;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KeeperData.Application.Tests.Unit.Commands.UserAccounts;

public class EnsureUserAccountCommandHandlerTests
{
    private const string Subject = "9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d";
    private const string Email = "jane.farmer@example.com";

    private readonly CancellationToken _token = CancellationToken.None;
    private readonly Mock<IUserAccountsRepository> _repository = new();
    private readonly Mock<IUserAccountAssociationBuilder> _associationBuilder = new();
    private readonly EnsureUserAccountCommandHandler _sut;

    public EnsureUserAccountCommandHandlerTests()
    {
        _associationBuilder
            .Setup(x => x.BuildForEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _sut = new EnsureUserAccountCommandHandler(
            _repository.Object,
            _associationBuilder.Object,
            NullLogger<EnsureUserAccountCommandHandler>.Instance);
    }

    [Fact]
    public async Task GivenAnUnknownSubjectAndUnknownEmail_WhenEnsuring_ThenAnAccountIsCreated()
    {
        var result = await _sut.Handle(Command(), _token);

        result.Created.Should().BeTrue();
        result.Account.Subject.Should().Be(Subject);
        result.Account.Email.Should().Be(Email);
        result.Account.DisplayName.Should().Be("Jane Farmer");

        _repository.Verify(x => x.AddAsync(It.IsAny<UserAccountDocument>(), _token), Times.Once);
        _repository.Verify(x => x.UpdateAsync(It.IsAny<UserAccountDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAKnownSubject_WhenEnsuring_ThenProfileFieldsAreOverwritten()
    {
        SetupExistingAccountBySubject(new UserAccountDocument
        {
            Id = "account-id",
            Subject = Subject,
            Email = "stale@example.com",
            FirstName = "Stale",
            LastName = "Name",
            DisplayName = "Stale Name"
        });

        var result = await _sut.Handle(Command(), _token);

        result.Created.Should().BeFalse();
        result.Account.Id.Should().Be("account-id");
        result.Account.Email.Should().Be(Email);
        result.Account.FirstName.Should().Be("Jane");
        result.Account.LastName.Should().Be("Farmer");
        result.Account.DisplayName.Should().Be("Jane Farmer");

        _repository.Verify(x => x.UpdateAsync(It.IsAny<UserAccountDocument>(), _token), Times.Once);
    }

    [Fact]
    public async Task GivenAnAccountWithNoSubjectMatchingOnEmail_WhenEnsuring_ThenTheAccountIsAdopted()
    {
        SetupExistingAccountByEmail(new UserAccountDocument
        {
            Id = "invited-account-id",
            Subject = null,
            Email = Email
        });

        var result = await _sut.Handle(Command(), _token);

        result.Created.Should().BeFalse();
        result.Account.Id.Should().Be("invited-account-id");
        result.Account.Subject.Should().Be(Subject);

        _repository.Verify(x => x.UpdateAsync(It.IsAny<UserAccountDocument>(), _token), Times.Once);
    }

    [Fact]
    public async Task GivenAnAccountWithADifferentSubjectMatchingOnEmail_WhenEnsuring_ThenAConflictIsThrown()
    {
        SetupExistingAccountByEmail(new UserAccountDocument
        {
            Id = "other-account-id",
            Subject = "another-subject",
            Email = Email
        });

        var act = () => _sut.Handle(Command(), _token);

        await act.Should().ThrowAsync<ConflictException>();

        _repository.Verify(x => x.AddAsync(It.IsAny<UserAccountDocument>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(x => x.UpdateAsync(It.IsAny<UserAccountDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAKnownSubject_WhenTheClaimsEmailBelongsToADifferentAccount_ThenAConflictIsThrown()
    {
        SetupExistingAccountBySubject(new UserAccountDocument
        {
            Id = "account-id",
            Subject = Subject,
            Email = "stale@example.com"
        });

        _repository
            .Setup(x => x.FindByEmailAsync(Email, _token))
            .ReturnsAsync(new UserAccountDocument
            {
                Id = "other-account-id",
                Subject = "another-subject",
                Email = Email
            });

        var act = () => _sut.Handle(Command(), _token);

        await act.Should().ThrowAsync<ConflictException>();

        _repository.Verify(x => x.UpdateAsync(It.IsAny<UserAccountDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingAssociations_WhenTheRebuildReturnsNothing_ThenTheSnapshotIsEmptied()
    {
        SetupExistingAccountBySubject(new UserAccountDocument
        {
            Id = "account-id",
            Subject = Subject,
            Email = Email,
            CphAssociations =
            [
                new CphAssociationDocument { IdentifierId = "stale", CphNumber = "12/345/6789", Role = "owner" }
            ]
        });

        var result = await _sut.Handle(Command(), _token);

        result.Account.CphAssociations.Should().BeEmpty();
        result.Account.AssociationsRefreshedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GivenARebuiltGraph_WhenEnsuring_ThenStaleAssociationsAreReplaced()
    {
        SetupExistingAccountBySubject(new UserAccountDocument
        {
            Id = "account-id",
            Subject = Subject,
            Email = Email,
            CphAssociations =
            [
                new CphAssociationDocument { IdentifierId = "stale", CphNumber = "12/345/6789", Role = "owner" }
            ]
        });

        _associationBuilder
            .Setup(x => x.BuildForEmailAsync(Email, _token))
            .ReturnsAsync([new CphAssociationDocument { IdentifierId = "fresh", CphNumber = "57/103/2335", Role = "owner" }]);

        var result = await _sut.Handle(Command(), _token);

        result.Account.CphAssociations.Should().HaveCount(1);
        result.Account.CphAssociations[0].CphNumber.Should().Be("57/103/2335");
    }

    private static EnsureUserAccountCommand Command() => new(Subject, Email, "Jane", "Farmer");

    private void SetupExistingAccountBySubject(UserAccountDocument account) =>
        _repository.Setup(x => x.FindBySubjectAsync(Subject, _token)).ReturnsAsync(account);

    private void SetupExistingAccountByEmail(UserAccountDocument account) =>
        _repository.Setup(x => x.FindByEmailAsync(Email, _token)).ReturnsAsync(account);
}