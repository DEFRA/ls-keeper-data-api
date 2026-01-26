using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Infrastructure.Database.Repositories;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class SitesRepositoryTests
{
    private readonly MockMongoDatabase _mockDb = new();
    private readonly SitesRepository _sut;

    public SitesRepositoryTests()
    {
        _mockDb.SetupCollection<SiteDocument>();
        _sut = new SitesRepository(_mockDb.Config, _mockDb.Client, _mockDb.UnitOfWork);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnDocumentCount_WhenCalled()
    {
        var filter = Builders<SiteDocument>.Filter.Empty;
        var expectedCount = 5L;

        _mockDb.MockCollection<SiteDocument>()
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<SiteDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var result = await _sut.CountAsync(filter);

        result.Should().Be(5);
    }

    [Fact]
    public async Task CountAsync_ShouldUseCancellationToken_WhenProvided()
    {
        var filter = Builders<SiteDocument>.Filter.Empty;
        var cancellationToken = new CancellationToken();
        var expectedCount = 3L;

        _mockDb.MockCollection<SiteDocument>()
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<SiteDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var result = await _sut.CountAsync(filter, cancellationToken);

        result.Should().Be(3);
        
        _mockDb.MockCollection<SiteDocument>()
            .Verify(x => x.CountDocumentsAsync(
            It.IsAny<FilterDefinition<SiteDocument>>(),
            It.IsAny<CountOptions>(),
            cancellationToken), Times.Once);
    }
}