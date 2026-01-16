using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class SpeciesRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<SpeciesRepository, SpeciesListDocument, SpeciesDocument> _fixture;
    private readonly SpeciesRepository _sut;

    public SpeciesRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<SpeciesRepository, SpeciesListDocument, SpeciesDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new SpeciesRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_ReturnsMatchingSpecies()
    {
        var bovineId = Guid.NewGuid().ToString();
        var porcineId = Guid.NewGuid().ToString();
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = porcineId, Code = "POR", Name = "Porcine", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(bovineId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(bovineId);
        result.Code.Should().Be("BOV");
        result.Name.Should().Be("Bovine");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_IsCaseInsensitive()
    {
        var bovineId = Guid.NewGuid().ToString();
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(bovineId.ToUpper(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(bovineId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WhenCalledWithNullOrEmpty_ReturnsNull(string? id)
    {
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenSpeciesNotFound_ReturnsNull()
    {
        var bovineId = Guid.NewGuid().ToString();
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);
        var nonExistentId = Guid.NewGuid().ToString();

        var result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidCode_ReturnsSpeciesIdAndName()
    {
        var bovineId = Guid.NewGuid().ToString();
        var porcineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = porcineId, Code = "POR", Name = "Porcine", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("BOV", CancellationToken.None);

        result.speciesId.Should().Be(bovineId);
        result.speciesName.Should().Be("Bovine");
    }

    [Fact]
    public async Task FindAsync_WhenCodeNotFoundButNameMatches_ReturnsSpeciesIdAndName()
    {
        var bovineId = Guid.NewGuid().ToString();
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Bovine", CancellationToken.None);

        result.speciesId.Should().Be(bovineId);
        result.speciesName.Should().Be("Bovine");
    }

    [Fact]
    public async Task FindAsync_WhenMatchingByCodeOrName_IsCaseInsensitive()
    {
        var bovineId = Guid.NewGuid().ToString();
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);

        var resultByCode = await _sut.FindAsync("bov", CancellationToken.None);
        var resultByName = await _sut.FindAsync("bovine", CancellationToken.None);

        resultByCode.speciesId.Should().Be(bovineId);
        resultByName.speciesId.Should().Be(bovineId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_WhenCalledWithNullOrEmpty_ReturnsNulls(string? lookupValue)
    {
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        result.speciesId.Should().BeNull();
        result.speciesName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenSpeciesNotFound_ReturnsNulls()
    {
        var bovineId = Guid.NewGuid().ToString();
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Unknown", CancellationToken.None);

        result.speciesId.Should().BeNull();
        result.speciesName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchExists_PrefersCodeOverName()
    {
        var bovineId = Guid.NewGuid().ToString();
        var otherId = Guid.NewGuid().ToString();
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Cattle", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = otherId, Code = "OTHER", Name = "BOV", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("BOV", CancellationToken.None);

        result.speciesId.Should().Be(bovineId);
        result.speciesName.Should().Be("Cattle");
    }
}