using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class CountryRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<CountryRepository, CountryListDocument, CountryDocument> _fixture;
    private readonly CountryRepository _sut;

    public CountryRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<CountryRepository, CountryListDocument, CountryDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new CountryRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_ReturnsMatchingCountry()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(gbId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(gbId);
        result.Code.Should().Be("GB");
        result.Name.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_IsCaseInsensitive()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(gbId.ToUpper(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(gbId);
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
    public async Task GetByIdAsync_WhenCountryNotFound_ReturnsNull()
    {
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

        _fixture.SetUpDocuments(listDocument);
        var nonExistentId = Guid.NewGuid().ToString();

        var result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidCode_ReturnsCountryIdAndName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("GB", CancellationToken.None);

        result.countryId.Should().Be(gbId);
        result.countryName.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task FindAsync_WhenCodeNotFoundButNameMatches_ReturnsCountryIdAndName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("United Kingdom", CancellationToken.None);

        result.countryId.Should().Be(gbId);
        result.countryName.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task FindAsync_WhenMatchingByCodeOrName_IsCaseInsensitive()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var resultByCode = await _sut.FindAsync("gb", CancellationToken.None);
        var resultByName = await _sut.FindAsync("united kingdom", CancellationToken.None);

        resultByCode.countryId.Should().Be(gbId);
        resultByName.countryId.Should().Be(gbId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_WhenCalledWithNullOrEmpty_ReturnsNulls(string? lookupValue)
    {
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        result.countryId.Should().BeNull();
        result.countryName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCountryNotFound_ReturnsNulls()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Unknown", CancellationToken.None);

        result.countryId.Should().BeNull();
        result.countryName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchExists_PrefersCodeOverName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("GB", CancellationToken.None);

        result.countryId.Should().Be(gbId);
        result.countryName.Should().Be("Great Britain");
    }
}