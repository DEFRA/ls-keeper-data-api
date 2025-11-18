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

public class ProductionUsageRepositoryTests
{
    private readonly Mock<IOptions<MongoConfig>> _configMock;
    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock;
    private readonly Mock<IMongoCollection<ProductionUsageListDocument>> _collectionMock;
    private readonly Mock<IAsyncCursor<ProductionUsageListDocument>> _asyncCursorMock;
    private readonly ProductionUsageRepository _sut;

    public ProductionUsageRepositoryTests()
    {
        _configMock = new Mock<IOptions<MongoConfig>>();
        _mongoClientMock = new Mock<IMongoClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clientSessionHandleMock = new Mock<IClientSessionHandle>();
        _collectionMock = new Mock<IMongoCollection<ProductionUsageListDocument>>();
        _asyncCursorMock = new Mock<IAsyncCursor<ProductionUsageListDocument>>();

        _configMock.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "test" });

        var mockDatabase = new Mock<IMongoDatabase>();
        _mongoClientMock.Setup(c => c.GetDatabase(It.IsAny<string>(), null)).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.GetCollection<ProductionUsageListDocument>(It.IsAny<string>(), null))
            .Returns(_collectionMock.Object);

        _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() =>
            {
                _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            });

        _collectionMock.Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<ProductionUsageListDocument>>(),
                It.IsAny<FindOptions<ProductionUsageListDocument, ProductionUsageListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session).Returns(_clientSessionHandleMock.Object);

        _sut = new ProductionUsageRepository(_configMock.Object, _mongoClientMock.Object, _unitOfWorkMock.Object);

        var collectionField = typeof(ProductionUsageRepository).BaseType!.BaseType!
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
        collectionField!.SetValue(_sut, _collectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingProductionUsage()
    {
        // Arrange
        var approvedId = "40faaff4-0004-4f8d-94c8-04c461724598";
        var beefId = "ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824";

        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = approvedId,
                Code = "APPROVED",
                Description = "Approved Pyramid",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = beefId,
                Code = "BEEF",
                Description = "Beef",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(approvedId);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(approvedId);
        result.Code.Should().Be("APPROVED");
        result.Description.Should().Be("Approved Pyramid");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingProductionUsage()
    {
        // Arrange
        var approvedId = "40faaff4-0004-4f8d-94c8-04c461724598";

        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = approvedId,
                Code = "APPROVED",
                Description = "Approved Pyramid",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync("40FAAFF4-0004-4F8D-94C8-04C461724598");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("APPROVED");
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
        // Arrange
        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = "40faaff4-0004-4f8d-94c8-04c461724598",
                Code = "APPROVED",
                Description = "Approved Pyramid",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
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
    public async Task FindAsync_WhenCalledWithMatchingCode_ReturnsCodeAndDescription()
    {
        // Arrange
        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = "40faaff4-0004-4f8d-94c8-04c461724598",
                Code = "APPROVED",
                Description = "Approved Pyramid",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("APPROVED");

        // Assert
        result.productionUsageId.Should().Be("APPROVED");
        result.productionUsageDescription.Should().Be("Approved Pyramid");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingDescription_ReturnsCodeAndDescription()
    {
        // Arrange
        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = "add70003-34ad-4020-90ae-bd6d20f58f15",
                Code = "CALFREAR",
                Description = "Calf Rearer",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Calf Rearer");

        // Assert
        result.productionUsageId.Should().Be("CALFREAR");
        result.productionUsageDescription.Should().Be("Calf Rearer");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndDescription()
    {
        // Arrange
        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = "ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824",
                Code = "BEEF",
                Description = "Beef",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("beef");

        // Assert
        result.productionUsageId.Should().Be("BEEF");
        result.productionUsageDescription.Should().Be("Beef");
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
        // Arrange
        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = "40faaff4-0004-4f8d-94c8-04c461724598",
                Code = "APPROVED",
                Description = "Approved Pyramid",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("NONEXISTENT");

        // Assert
        result.productionUsageId.Should().BeNull();
        result.productionUsageDescription.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenLookupValueIsNull_ReturnsNullTuple()
    {
        // Arrange & Act
        var result = await _sut.FindAsync(null);

        // Assert
        result.productionUsageId.Should().BeNull();
        result.productionUsageDescription.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndDescriptionAlsoExists_PrioritizesCodeMatch()
    {
        // Arrange
        var productionUsages = new List<ProductionUsageDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Code = "BEEF",
                Description = "Beef",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                Code = "APPROVED",
                Description = "BEEF",
                IsActive = true,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new ProductionUsageListDocument
        {
            Id = "all-productionusages",
            ProductionUsages = productionUsages
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("BEEF");

        // Assert
        result.productionUsageId.Should().Be("BEEF");
        result.productionUsageDescription.Should().Be("Beef");
    }
}