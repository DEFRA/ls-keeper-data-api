using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class SiteIdentifierTypeRepositoryTests
{
    private readonly Mock<IOptions<MongoConfig>> _configMock;
    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock;
    private readonly Mock<IMongoCollection<SiteIdentifierTypeListDocument>> _collectionMock;
    private readonly Mock<IAsyncCursor<SiteIdentifierTypeListDocument>> _asyncCursorMock;
    private readonly SiteIdentifierTypeRepository _sut;

    public SiteIdentifierTypeRepositoryTests()
    {
        _configMock = new Mock<IOptions<MongoConfig>>();
        _mongoClientMock = new Mock<IMongoClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clientSessionHandleMock = new Mock<IClientSessionHandle>();
        _collectionMock = new Mock<IMongoCollection<SiteIdentifierTypeListDocument>>();
        _asyncCursorMock = new Mock<IAsyncCursor<SiteIdentifierTypeListDocument>>();

        _configMock.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "test" });

        var mockDatabase = new Mock<IMongoDatabase>();
        _mongoClientMock.Setup(c => c.GetDatabase(It.IsAny<string>(), null)).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.GetCollection<SiteIdentifierTypeListDocument>(It.IsAny<string>(), null))
            .Returns(_collectionMock.Object);

        _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() =>
            {
                _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            });

        _collectionMock.Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<SiteIdentifierTypeListDocument>>(),
                It.IsAny<FindOptions<SiteIdentifierTypeListDocument, SiteIdentifierTypeListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session).Returns(_clientSessionHandleMock.Object);

        _sut = new SiteIdentifierTypeRepository(_configMock.Object, _mongoClientMock.Object, _unitOfWorkMock.Object);

        var collectionField = typeof(SiteIdentifierTypeRepository).BaseType!.BaseType!
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
        collectionField!.SetValue(_sut, _collectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingSiteIdentifierType()
    {
        // Arrange
        var cphnId = "6b4ca299-895d-4cdb-95dd-670de71ff328";
        var fsanId = "cb2fb3ee-6368-4125-a413-fc905fec51f0";

        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = cphnId,
                Code = "CPHN",
                Name = "CPH Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = fsanId,
                Code = "FSAN",
                Name = "FSA Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(cphnId);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(cphnId);
        result.Code.Should().Be("CPHN");
        result.Name.Should().Be("CPH Number");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingSiteIdentifierType()
    {
        // Arrange
        var cphnId = "6b4ca299-895d-4cdb-95dd-670de71ff328";

        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = cphnId,
                Code = "CPHN",
                Name = "CPH Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync("6B4CA299-895D-4CDB-95DD-670DE71FF328");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("CPHN");
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
        // Arrange
        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Code = "CPHN",
                Name = "CPH Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsNull_ReturnsNull()
    {
        // Arrange & Act
        var result = await _sut.GetByIdAsync(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsWhitespace_ReturnsNull()
    {
        // Arrange & Act
        var result = await _sut.GetByIdAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingCode_ReturnsCodeAndName()
    {
        // Arrange
        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Code = "CPHN",
                Name = "CPH Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("CPHN");

        // Assert
        result.siteIdentifierTypeId.Should().Be("6b4ca299-895d-4cdb-95dd-670de71ff328");
        result.siteIdentifierTypeName.Should().Be("CPH Number");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingName_ReturnsCodeAndName()
    {
        // Arrange
        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = "4e135625-2d31-46ce-b9fe-93bc70ad6395",
                Code = "PRTN",
                Name = "Port Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Port Number");

        // Assert
        result.siteIdentifierTypeId.Should().Be("4e135625-2d31-46ce-b9fe-93bc70ad6395");
        result.siteIdentifierTypeName.Should().Be("Port Number");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndName()
    {
        // Arrange
        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = "cb2fb3ee-6368-4125-a413-fc905fec51f0",
                Code = "FSAN",
                Name = "FSA Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("fsan");

        // Assert
        result.siteIdentifierTypeId.Should().Be("cb2fb3ee-6368-4125-a413-fc905fec51f0");
        result.siteIdentifierTypeName.Should().Be("FSA Number");
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
        // Arrange
        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Code = "CPHN",
                Name = "CPH Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("NONEXISTENT");

        // Assert
        result.siteIdentifierTypeId.Should().BeNull();
        result.siteIdentifierTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenLookupValueIsNull_ReturnsNullTuple()
    {
        // Arrange & Act
        var result = await _sut.FindAsync(null);

        // Assert
        result.siteIdentifierTypeId.Should().BeNull();
        result.siteIdentifierTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndNameAlsoExists_PrioritizesCodeMatch()
    {
        // Arrange
        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Code = "CPHN",
                Name = "CPH Number",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                Code = "FSAN",
                Name = "CPHN",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new SiteIdentifierTypeListDocument
        {
            Id = "all-siteidentifiertypes",
            SiteIdentifierTypes = siteIdentifierTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("CPHN");

        // Assert
        result.siteIdentifierTypeId.Should().Be("id1");
        result.siteIdentifierTypeName.Should().Be("CPH Number");
    }
}