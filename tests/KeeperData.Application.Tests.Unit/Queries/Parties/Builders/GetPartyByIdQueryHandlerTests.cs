using FluentAssertions;
using KeeperData.Application.Queries.Parties;
using KeeperData.Core.Documents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Parties.Builders;

public class GetPartyByIdQueryHandlerTests
{
    private readonly CancellationToken Token = new CancellationToken();
    private const string PartyIdThatExists = "party-id";
    private const string PartyIdThatDoesNotExist = "bad-id";
    private Mock<IGenericRepository<PartyDocument>> mockRepo;
    private GetPartyByIdQueryHandler sut;

    public GetPartyByIdQueryHandlerTests()
    {
        mockRepo = new Mock<IGenericRepository<PartyDocument>>();
        sut = new GetPartyByIdQueryHandler(mockRepo.Object);
    }

    [Fact]
    public async Task GivenPartyDoesNotExistWhenGettingDocumentItShouldThrowNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(new GetPartyByIdQuery(PartyIdThatDoesNotExist), Token));
    }

    [Fact]
    public async Task GivenPartyDoesExistWhenGettingDocumentItShouldReturnRequestedDocument()
    {
        var expectedDocument = new PartyDocument { Id = PartyIdThatExists };
        mockRepo.Setup(x => x.GetByIdAsync(PartyIdThatExists, Token)).Returns(Task.FromResult(expectedDocument));

        var result = await sut.Handle(new GetPartyByIdQuery(PartyIdThatExists), Token);

        result.Should().Be(expectedDocument);
    }
}