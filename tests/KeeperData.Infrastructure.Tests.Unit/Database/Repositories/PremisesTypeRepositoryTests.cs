using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class PremisesTypeRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<PremisesTypeRepository, PremisesTypeListDocument, PremisesTypeDocument> _fixture;
    private readonly PremisesTypeRepository _sut;

    public PremisesTypeRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<PremisesTypeRepository, PremisesTypeListDocument, PremisesTypeDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new PremisesTypeRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingPremisesType()
    {
        var acId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a";
        var maId = "491cbd98-5bb7-46c3-abc7-30a232f65043";
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = acId,
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = maId,
                Code = "MA",
                Name = "Market",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(acId);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(acId);
        result.Code.Should().Be("AC");
        result.Name.Should().Be("Assembly Centre");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingPremisesType()
    {
        var acId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a";
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = acId,
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("1DBD0EA4-5F10-45A4-A0F6-E328A3074B6A");

        result.Should().NotBeNull();
        result!.Code.Should().Be("AC");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WhenCalledWithNullOrWhitespace_ReturnsNull(string? id)
    {
        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("non-existent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingCode_ReturnsCodeAndName()
    {
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("AC");

        result.premiseTypeId.Should().Be("1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a");
        result.premiseTypeName.Should().Be("Assembly Centre");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingName_ReturnsCodeAndName()
    {
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "491cbd98-5bb7-46c3-abc7-30a232f65043",
                Code = "MA",
                Name = "Market",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Market");

        result.premiseTypeId.Should().Be("491cbd98-5bb7-46c3-abc7-30a232f65043");
        result.premiseTypeName.Should().Be("Market");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndName()
    {
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("ac");

        result.premiseTypeId.Should().Be("1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a");
        result.premiseTypeName.Should().Be("Assembly Centre");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_WhenCalledWithNullOrWhitespace_ReturnsNullTuple(string? lookupValue)
    {
        var result = await _sut.FindAsync(lookupValue);

        result.premiseTypeId.Should().BeNull();
        result.premiseTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("NONEXISTENT");

        result.premiseTypeId.Should().BeNull();
        result.premiseTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndNameAlsoExists_PrioritizesCodeMatch()
    {
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Code = "MA",
                Name = "Market",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                Code = "AC",
                Name = "MA",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("MA");

        result.premiseTypeId.Should().Be("id1");
        result.premiseTypeName.Should().Be("Market");
    }
}