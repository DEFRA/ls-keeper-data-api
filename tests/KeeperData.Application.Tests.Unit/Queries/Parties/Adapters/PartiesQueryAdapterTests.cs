using FluentAssertions;
using KeeperData.Application.Queries.Parties;
using KeeperData.Application.Queries.Parties.Adapters;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Parties.Adapters;

public class PartiesQueryAdapterTests
{
    [Fact]
    public async Task GetPartiesAsync_ShouldCallRepositoryWithCorrectParameters()
    {
        var repositoryMock = new Mock<IPartiesRepository>();
        var adapter = new PartiesQueryAdapter(repositoryMock.Object);

        var query = new GetPartiesQuery
        {
            FirstName = "John",
            Page = 2,
            PageSize = 10,
            Order = "firstName",
            Sort = "desc"
        };

        var expectedItems = new List<PartyDocument> { new PartyDocument { Id = "1" } };

        repositoryMock.Setup(x => x.CountAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        repositoryMock.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<PartyDocument>>(),
            It.IsAny<SortDefinition<PartyDocument>>(),
            10,
            10,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var (items, count) = await adapter.GetPartiesAsync(query);

        items.Should().BeEquivalentTo(expectedItems);
        count.Should().Be(1);

        repositoryMock.Verify(x => x.FindAsync(
            It.IsAny<FilterDefinition<PartyDocument>>(),
            It.IsAny<SortDefinition<PartyDocument>>(),
            10, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}