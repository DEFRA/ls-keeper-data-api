using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class PremisesActivityTypeRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<PremisesActivityTypeRepository, PremisesActivityTypeListDocument, PremisesActivityTypeDocument> _fixture;
    private readonly PremisesActivityTypeRepository _sut;

    public PremisesActivityTypeRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<PremisesActivityTypeRepository, PremisesActivityTypeListDocument, PremisesActivityTypeDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new PremisesActivityTypeRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingPremisesActivityType()
    {
        var marpId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab";
        var maruId = "89e6b48b-4aee-4a0f-9c0b-58e9aa6c3fb2";
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = marpId,
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = maruId,
                Code = "MARU",
                Name = "Market on Unpaved Ground",
                IsActive = true,
                PriorityOrder = 20,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(marpId);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(marpId);
        result.Code.Should().Be("MARP");
        result.Name.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingPremisesActivityType()
    {
        var marpId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab";
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = marpId,
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("66D885C0-CE67-4CB5-8FD2-DD1F70A3C0AB");

        result.Should().NotBeNull();
        result!.Code.Should().Be("MARP");
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
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
    public async Task FindAsync_WhenCalledWithMatchingCode_ReturnsCodeAndName()
    {
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("MARP");

        result.premiseActivityTypeId.Should().Be("66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab");
        result.premiseActivityTypeName.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingName_ReturnsCodeAndName()
    {
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "5e22d572-c98e-4892-98f7-c02c6eb37224",
                Code = "CC",
                Name = "Collection Centre",
                IsActive = true,
                PriorityOrder = 30,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Collection Centre");

        result.premiseActivityTypeId.Should().Be("5e22d572-c98e-4892-98f7-c02c6eb37224");
        result.premiseActivityTypeName.Should().Be("Collection Centre");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndName()
    {
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("marp");

        result.premiseActivityTypeId.Should().Be("66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab");
        result.premiseActivityTypeName.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("NONEXISTENT");

        result.premiseActivityTypeId.Should().BeNull();
        result.premiseActivityTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenLookupValueIsNull_ReturnsNullTuple()
    {
        var result = await _sut.FindAsync(null);

        result.premiseActivityTypeId.Should().BeNull();
        result.premiseActivityTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndNameAlsoExists_PrioritizesCodeMatch()
    {
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Code = "CC",
                Name = "Collection Centre",
                IsActive = true,
                PriorityOrder = 30,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                Code = "MARP",
                Name = "CC",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("CC");

        result.premiseActivityTypeId.Should().Be("id1");
        result.premiseActivityTypeName.Should().Be("Collection Centre");
    }
}