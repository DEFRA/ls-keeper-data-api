using FluentAssertions;
using KeeperData.Application.Queries.UserAccounts;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.UserAccounts;

public class GetUserAccountBySubjectQueryHandlerTests
{
    private const string KnownSubject = "known-subject";
    private const string UnknownSubject = "unknown-subject";

    private readonly CancellationToken _token = CancellationToken.None;
    private readonly Mock<IUserAccountsRepository> _repository = new();
    private readonly GetUserAccountBySubjectQueryHandler _sut;

    public GetUserAccountBySubjectQueryHandlerTests()
    {
        _sut = new GetUserAccountBySubjectQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task GivenAnUnknownSubject_WhenQuerying_ThenNotFoundIsThrown()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.Handle(new GetUserAccountBySubjectQuery(UnknownSubject), _token));
    }

    [Fact]
    public async Task GivenAKnownSubject_WhenQuerying_ThenTheAccountSnapshotIsReturned()
    {
        _repository
            .Setup(x => x.FindBySubjectAsync(KnownSubject, _token))
            .ReturnsAsync(new UserAccountDocument
            {
                Id = "account-id",
                Subject = KnownSubject,
                Email = "jane.farmer@example.com",
                DisplayName = "Jane Farmer",
                CphAssociations =
                [
                    new CphAssociationDocument { IdentifierId = "identifier-id", CphNumber = "57/103/2335", Role = "owner" }
                ]
            });

        var result = await _sut.Handle(new GetUserAccountBySubjectQuery(KnownSubject), _token);

        result.Subject.Should().Be(KnownSubject);
        result.DisplayName.Should().Be("Jane Farmer");
        result.CphAssociations.Should().HaveCount(1);
        result.CphAssociations[0].CphNumber.Should().Be("57/103/2335");
    }
}
