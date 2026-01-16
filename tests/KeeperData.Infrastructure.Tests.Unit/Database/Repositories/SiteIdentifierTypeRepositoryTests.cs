using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class SiteIdentifierTypeRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<SiteIdentifierTypeRepository, SiteIdentifierTypeListDocument, SiteIdentifierTypeDocument> _fixture;
    private readonly SiteIdentifierTypeRepository _sut;

    public SiteIdentifierTypeRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<SiteIdentifierTypeRepository, SiteIdentifierTypeListDocument, SiteIdentifierTypeDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new SiteIdentifierTypeRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingSiteIdentifierType()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(cphnId);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(cphnId);
        result.Code.Should().Be("CPHN");
        result.Name.Should().Be("CPH Number");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingSiteIdentifierType()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync("6B4CA299-895D-4CDB-95DD-670DE71FF328");

        result.Should().NotBeNull();
        result!.Code.Should().Be("CPHN");
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("CPHN");

        result.siteIdentifierTypeId.Should().Be("6b4ca299-895d-4cdb-95dd-670de71ff328");
        result.siteIdentifierTypeName.Should().Be("CPH Number");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingName_ReturnsCodeAndName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Port Number");

        result.siteIdentifierTypeId.Should().Be("4e135625-2d31-46ce-b9fe-93bc70ad6395");
        result.siteIdentifierTypeName.Should().Be("Port Number");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("fsan");

        result.siteIdentifierTypeId.Should().Be("cb2fb3ee-6368-4125-a413-fc905fec51f0");
        result.siteIdentifierTypeName.Should().Be("FSA Number");
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("NONEXISTENT");

        result.siteIdentifierTypeId.Should().BeNull();
        result.siteIdentifierTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenLookupValueIsNull_ReturnsNullTuple()
    {
        var result = await _sut.FindAsync(null);

        result.siteIdentifierTypeId.Should().BeNull();
        result.siteIdentifierTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndNameAlsoExists_PrioritizesCodeMatch()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("CPHN");

        result.siteIdentifierTypeId.Should().Be("id1");
        result.siteIdentifierTypeName.Should().Be("CPH Number");
    }
}