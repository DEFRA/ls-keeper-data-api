using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class CountryRepositoryTests
{
    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<CountryListDocument>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<CountryListDocument>> _mongoCollectionMock = new();

    private readonly CountryRepository _sut;

    public CountryRepositoryTests()
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "TestDatabase" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<CountryListDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_mongoCollectionMock.Object);

        _mongoClientMock
            .Setup(client => client.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(_mongoDatabaseMock.Object);

        _asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mongoCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<CountryListDocument>>(),
                It.IsAny<FindOptions<CountryListDocument, CountryListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session)
            .Returns(_clientSessionHandleMock.Object);

        _sut = new CountryRepository(_mongoConfig, _mongoClientMock.Object, _unitOfWorkMock.Object);

        typeof(GenericRepository<CountryListDocument>)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, _mongoCollectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_ReturnsMatchingCountry()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();
        var frId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = frId, Code = "FR", Name = "France", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(gbId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(gbId);
        result.Code.Should().Be("GB");
        result.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_IsCaseInsensitive()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(gbId.ToUpper(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(gbId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WhenCalledWithNullOrEmpty_ReturnsNull(string? id)
    {
        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCountryNotFound_ReturnsNull()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidCode_ReturnsCountryIdAndName()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();
        var frId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = frId, Code = "FR", Name = "France", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("GB", CancellationToken.None);

        // Assert
        result.countryId.Should().Be("GB");
        result.countryName.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task FindAsync_WhenCodeNotFoundButNameMatches_ReturnsCountryIdAndName()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("United Kingdom", CancellationToken.None);

        // Assert
        result.countryId.Should().Be("GB");
        result.countryName.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task FindAsync_WhenMatchingByCodeOrName_IsCaseInsensitive()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var resultByCode = await _sut.FindAsync("gb", CancellationToken.None);
        var resultByName = await _sut.FindAsync("united kingdom", CancellationToken.None);

        // Assert
        resultByCode.countryId.Should().Be("GB");
        resultByName.countryId.Should().Be("GB");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_WhenCalledWithNullOrEmpty_ReturnsNulls(string? lookupValue)
    {
        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.countryId.Should().BeNull();
        result.countryName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCountryNotFound_ReturnsNulls()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Unknown", CancellationToken.None);

        // Assert
        result.countryId.Should().BeNull();
        result.countryName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchExists_PrefersCodeOverName()
    {
        // Arrange
        var gbId = Guid.NewGuid().ToString();
        var otherId = Guid.NewGuid().ToString();

        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = gbId, Code = "GB", Name = "Great Britain", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = otherId, Code = "OTHER", Name = "GB", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countries
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("GB", CancellationToken.None);

        // Assert
        result.countryId.Should().Be("GB");
        result.countryName.Should().Be("Great Britain");
    }
}