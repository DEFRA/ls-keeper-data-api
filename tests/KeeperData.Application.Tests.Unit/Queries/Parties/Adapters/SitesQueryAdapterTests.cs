using FluentAssertions;
using KeeperData.Application.Queries.Sites;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Sites.Adapters;

public class SitesQueryAdapterTests
{
    [Fact]
    public async Task GetSitesAsync_ShouldCallRepositoryWithCorrectParameters()
    {
        var repositoryMock = new Mock<ISitesRepository>();
        var adapter = new SitesQueryAdapter(repositoryMock.Object);

        var query = new GetSitesQuery
        {
            SiteIdentifier = "CPH123",
            Page = 1,
            PageSize = 20
        };

        var expectedItems = new List<SiteDocument> { new SiteDocument { Id = "1" } };

        repositoryMock.Setup(x => x.CountAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        repositoryMock.Setup(x => x.FindAsync(
            It.IsAny<FilterDefinition<SiteDocument>>(),
            It.IsAny<SortDefinition<SiteDocument>>(),
            0,
            20,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var (items, count) = await adapter.GetSitesAsync(query);

        items.Should().BeEquivalentTo(expectedItems);
        count.Should().Be(5);
    }
}