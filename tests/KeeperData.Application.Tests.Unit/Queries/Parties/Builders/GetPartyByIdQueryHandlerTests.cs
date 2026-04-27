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
    public async Task GivenPartyDoesExistWhenGettingDocumentItShouldReturnDto()
    {
        var expectedDocument = new PartyDocument
        {
            Id = PartyIdThatExists,
            FirstName = "John",
            LastName = "Doe",
            Name = "John Doe",
            CustomerNumber = "C77473"
        };
        mockRepo.Setup(x => x.GetByIdAsync(PartyIdThatExists, Token)).ReturnsAsync(expectedDocument);

        var result = await sut.Handle(new GetPartyByIdQuery(PartyIdThatExists), Token);

        result.Should().NotBeNull();
        result.Id.Should().Be(PartyIdThatExists);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Name.Should().Be("John Doe");
        result.CustomerNumber.Should().Be("C77473");
    }
}