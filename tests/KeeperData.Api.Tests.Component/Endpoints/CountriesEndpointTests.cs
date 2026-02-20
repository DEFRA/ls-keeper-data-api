using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class CountriesEndpointTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [Fact]
    public async Task GetCountries_WithFilterParameters_ReturnsFilteredOkResult()
    {
        // Arrange
        var countries = new List<CountryDocument>
        {
            CreateCountry("GB", "United Kingdom", "United Kingdom of Great Britain and Northern Ireland", true, true),
            CreateCountry("IE", "Ireland", "Republic of Ireland", false, true),
            CreateCountry("US", "United States", "United States of America", false, false)
        };

        SetupRepository(countries);

        var query = "?name=United&euTradeMember=true&order=name&sort=asc";

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/countries{query}");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 1, expectedNames: ["United Kingdom"]);
    }

    [Fact]
    public async Task GetCountries_WithoutParameters_ReturnsDefaultOkResult()
    {
        // Arrange
        var countries = new List<CountryDocument>
        {
            CreateCountry("GB", "United Kingdom", "United Kingdom of Great Britain and Northern Ireland", true, true),
            CreateCountry("IE", "Ireland", "Republic of Ireland", false, true),
            CreateCountry("US", "United States", "United States of America", false, false)
        };

        SetupRepository(countries);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/countries");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 3, expectedNames: ["United Kingdom", "Ireland", "United States"]);
    }

    [Fact]
    public async Task GetCountries_WithCommaSeparatedCodes_ReturnsFilteredResult()
    {
        // Arrange
        var countries = new List<CountryDocument>
        {
            CreateCountry("GB", "United Kingdom", "United Kingdom of Great Britain and Northern Ireland", true, true),
            CreateCountry("IE", "Ireland", "Republic of Ireland", false, true),
            CreateCountry("US", "United States", "United States of America", false, false)
        };

        SetupRepository(countries);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/countries?code=GB,IE");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["United Kingdom", "Ireland"]);
    }

    [Fact]
    public async Task GetCountries_WithDevolvedAuthorityFilter_ReturnsFilteredResult()
    {
        // Arrange
        var countries = new List<CountryDocument>
        {
            CreateCountry("GB", "United Kingdom", "United Kingdom of Great Britain and Northern Ireland", true, true),
            CreateCountry("IE", "Ireland", "Republic of Ireland", false, true),
            CreateCountry("US", "United States", "United States of America", false, false)
        };

        SetupRepository(countries);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/countries?devolvedAuthority=true");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 1, expectedNames: ["United Kingdom"]);
    }

    [Fact]
    public async Task GetCountries_WithSortDescending_ReturnsCorrectOrder()
    {
        // Arrange
        var countries = new List<CountryDocument>
        {
            CreateCountry("IE", "Ireland", "Republic of Ireland", false, true),
            CreateCountry("GB", "United Kingdom", "United Kingdom of Great Britain and Northern Ireland", true, true),
            CreateCountry("US", "United States", "United States of America", false, false)
        };

        SetupRepository(countries);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/countries?order=name&sort=desc");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 3, expectedNames: ["United States", "United Kingdom", "Ireland"]);
    }

    [Fact]
    public async Task GetCountryById_WithValidId_ReturnsCountry()
    {
        // Arrange
        var countryId = Guid.NewGuid().ToString();
        var countries = new List<CountryDocument>
        {
            CreateCountry("GB", "United Kingdom", "United Kingdom of Great Britain and Northern Ireland", true, true, countryId)
        };

        SetupRepository(countries);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/countries/{countryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CountryDTO>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("United Kingdom");
        result.Code.Should().Be("GB");
        result.IdentifierId.Should().Be(countryId);
    }

    [Fact]
    public async Task GetCountryById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var countries = new List<CountryDocument>();
        SetupRepository(countries);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/countries/invalid-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CountryDocument CreateCountry(string code, string name, string longName, bool devolvedAuthority, bool euTradeMember, string? identifierId = null)
    {
        return new CountryDocument
        {
            IdentifierId = identifierId ?? Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            LongName = longName,
            DevolvedAuthority = devolvedAuthority,
            EuTradeMember = euTradeMember,
            LastModifiedDate = DateTime.UtcNow
        };
    }

    private void SetupRepository(List<CountryDocument> countries)
    {
        _appTestFixture.AppWebApplicationFactory._countryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(countries);

        _appTestFixture.AppWebApplicationFactory._countryRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) =>
                countries.FirstOrDefault(c => c.IdentifierId?.Equals(id, StringComparison.OrdinalIgnoreCase) == true));
    }

    private static async Task AssertPaginatedResponse(HttpResponseMessage response, int expectedCount, IEnumerable<string> expectedNames)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<CountryDTO>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(expectedCount);
        result.Values.Should().HaveCount(expectedCount);

        result.Values.Select(v => v.Name).Should().BeEquivalentTo(expectedNames, options => options.WithStrictOrdering());
    }
}