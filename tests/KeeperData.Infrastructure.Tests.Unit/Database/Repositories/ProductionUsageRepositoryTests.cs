using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class ProductionUsageRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<ProductionUsageRepository, ProductionUsageListDocument, ProductionUsageDocument> _fixture;
    private readonly ProductionUsageRepository _sut;

    public ProductionUsageRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<ProductionUsageRepository, ProductionUsageListDocument, ProductionUsageDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new ProductionUsageRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingProductionUsage()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(approvedId);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(approvedId);
        result.Code.Should().Be("APPROVED");
        result.Description.Should().Be("Approved Pyramid");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingProductionUsage()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("40FAAFF4-0004-4F8D-94C8-04C461724598");

        result.Should().NotBeNull();
        result!.Code.Should().Be("APPROVED");
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("non-existent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsNull_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsWhitespace_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("   ");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingCode_ReturnsCodeAndDescription()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("APPROVED");

        result.productionUsageId.Should().Be("40faaff4-0004-4f8d-94c8-04c461724598");
        result.productionUsageDescription.Should().Be("Approved Pyramid");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingDescription_ReturnsCodeAndDescription()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Calf Rearer");

        result.productionUsageId.Should().Be("add70003-34ad-4020-90ae-bd6d20f58f15");
        result.productionUsageDescription.Should().Be("Calf Rearer");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndDescription()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("beef");

        result.productionUsageId.Should().Be("ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824");
        result.productionUsageDescription.Should().Be("Beef");
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("NONEXISTENT");

        result.productionUsageId.Should().BeNull();
        result.productionUsageDescription.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenLookupValueIsNull_ReturnsNullTuple()
    {
        var result = await _sut.FindAsync(null);

        result.productionUsageId.Should().BeNull();
        result.productionUsageDescription.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndDescriptionAlsoExists_PrioritizesCodeMatch()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("BEEF");

        result.productionUsageId.Should().Be("id1");
        result.productionUsageDescription.Should().Be("Beef");
    }
}