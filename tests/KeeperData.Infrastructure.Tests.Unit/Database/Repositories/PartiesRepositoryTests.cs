using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Database.Repositories;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class PartiesRepositoryTests
{
    private readonly MockMongoDatabase _mockDb = new();
    private readonly PartiesRepository _sut;

    public PartiesRepositoryTests()
    {
        _mockDb.SetupCollection<PartyDocument>();
        _sut = new PartiesRepository(_mockDb.Config, _mockDb.Client, _mockDb.UnitOfWork);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnDocumentCount_WhenCalled()
    {
        var filter = Builders<PartyDocument>.Filter.Empty;
        var expectedCount = 7L;
        _mockDb.MockCollection<PartyDocument>()
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<PartyDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var result = await _sut.CountAsync(filter);
        
        result.Should().Be(7);
    }

    [Fact]
    public async Task CountAsync_ShouldUseCaseInsensitiveCollation_WhenCalled()
    {
        var filter = Builders<PartyDocument>.Filter.Empty;
        var expectedCount = 3L;
        _mockDb.MockCollection<PartyDocument>()
            .Setup(x => x.CountDocumentsAsync(
                filter,
                It.Is<CountOptions>(opts => opts.Collation == IndexDefaults.CollationCaseInsensitive),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var result = await _sut.CountAsync(filter);

        result.Should().Be(3);
        _mockDb.MockCollection<PartyDocument>()
            .Verify(x => x.CountDocumentsAsync(
            filter,
            It.Is<CountOptions>(opts => opts.Collation == IndexDefaults.CollationCaseInsensitive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountAsync_ShouldUseCancellationToken_WhenProvided()
    {
        var filter = Builders<PartyDocument>.Filter.Empty;
        var cancellationToken = new CancellationToken();
        var expectedCount = 5L;
        _mockDb.MockCollection<PartyDocument>()
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<PartyDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var result = await _sut.CountAsync(filter, cancellationToken);

        result.Should().Be(5);
        _mockDb.MockCollection<PartyDocument>()
            .Verify(x => x.CountDocumentsAsync(
            It.IsAny<FilterDefinition<PartyDocument>>(),
            It.IsAny<CountOptions>(),
            cancellationToken), Times.Once);
    }
}