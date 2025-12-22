using FluentAssertions;
using KeeperData.Application.Queries.Countries;
using KeeperData.Application.Queries.Countries.Adapters;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using MongoDB.Driver;
using Moq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;

namespace KeeperData.Core.Tests.Unit.Countries;

public class CountriesQueryAdapterTests
{
    private Mock<ICountryRepository> _countryRepoMock;
    private CountriesQueryAdapter _sut;
    public CountriesQueryAdapterTests()
    {
        _countryRepoMock = new Mock<ICountryRepository>();
        _sut = new CountriesQueryAdapter(_countryRepoMock.Object);
    }

    private static readonly DateTime GBLastUpdated2012Nov = new DateTime(2012, 08, 18, 11, 10, 0);
    private static readonly DateTime NZLastUpdated2013Nov = new DateTime(2013, 08, 18, 11, 10, 0);
    private static readonly DateTime FRLastUpdated2014Nov = new DateTime(2014, 08, 18, 11, 10, 0);

    private List<CountryDocument> TestCountries = new List<CountryDocument> {
            new() { IdentifierId = "GB-123", Code = "GB", Name = "UK", LongName = "United Kingdom", IsActive = true, DevolvedAuthority = true, EuTradeMember = false, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.MinValue, LastModifiedDate = GBLastUpdated2012Nov },
            new() { IdentifierId = "NZ-123", Code = "NZ", Name = "New Zealand", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow, LastModifiedDate = NZLastUpdated2013Nov },
            new() { IdentifierId = "FR-123", Code = "FR", Name = "France", LongName = "France", IsActive = true, SortOrder = 20, EuTradeMember = true, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow, LastModifiedDate = FRLastUpdated2014Nov },
        };

    private static CountryDTO CountryGBAsDTO = new CountryDTO { Code = "GB", IdentifierId = "GB-123", Name = "UK", LongName = "United Kingdom", DevolvedAuthorityFlag = true, EuTradeMemberFlag = false, LastUpdatedDate = GBLastUpdated2012Nov };
    private static CountryDTO CountryFRAsDTO = new CountryDTO { Code = "FR", IdentifierId = "FR-123", Name = "France", LongName = "France", DevolvedAuthorityFlag = false, EuTradeMemberFlag = true };

    [Fact]
    public async Task WhenHandlingQueryWithNoFilters_AllCountriesShouldBeReturned()
    {
        GivenTheseCountries(TestCountries);
        var result = await WhenSearchingForCountries();
        var expected = CountryGBAsDTO;

        result.Count().Should().Be(3);
        result.Should().Contain(x => x.Code == "GB");
        var gb = result.Single(x => x.Code == "GB");
        gb.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Search by Name", "France", null, null, null, null, null, "FR")]
    [InlineData("Search by Name should be contains string", "Zealand", null, null, null, null, null, "NZ")]
    [InlineData("Search by Name should ignore case", "uk", null, null, null, null, null, "GB")]
    [InlineData("Search by Name that doesnt exist", "NotRealCountry", null, null, null, null, null, null)]
    [InlineData("Search by One country code", null, "NZ", null, null, null, null, "NZ")]
    [InlineData("Search by multiple country codes", null, "FR,NZ", null, null, null, null, "FR,NZ")]
    [InlineData("Search by isEutrademember(true)", null, null, true, null, null, null, "FR")]
    [InlineData("Search by isEutrademember(false)", null, null, false, null, null, null, "GB,NZ")]
    [InlineData("Search by isDevolvedAuthority(true)", null, null, null, true, null, null, "GB")]
    [InlineData("Search by isDevolvedAuthority(false)", null, null, null, false, null, null, "FR,NZ")]
    [InlineData("Search by lastupdated", null, null, null, null, 2013, 11, "NZ,FR")]
    public async Task WhenUserSearchesAppropriateCountriesShouldBeReturned(string scenario, string? name, string? codesCsv, bool? euTradeMember, bool? devolvedAuthority, int? year, int? month, string? expectedCodes)
    {
        Debug.WriteLine(scenario);
        GivenTheseCountries(TestCountries);

        var result = await WhenSearchingForCountries(name, codesCsv, euTradeMember, devolvedAuthority, year.HasValue ? new DateTime(year.Value,month ?? 1,1) : null );

        var codes = expectedCodes?.Split(",") ?? new string[] { };
        result.Count().Should().Be(codes.Count());

        if (codes.Any())
            result.Select(c => c.Code).Should().BeEquivalentTo(codes);
    }

    [Theory]
    [InlineData("Sort by Name Asc", "name", "asc", null, null, "FR,NZ,GB")]
    [InlineData("Sort by Name Desc", "name", "desc", null, null, "GB,NZ,FR")]
    [InlineData("Sort by Code Asc", "code", "asc", null, null, "FR,GB,NZ")]
    [InlineData("Sort by Code Desc", "code", "desc", null, null, "NZ,GB,FR")]
    [InlineData("Default sort with paged (1-2 of 3)", null, null, 1, 2, "FR,GB")]
    [InlineData("Default sort with paged (3-3 of 3)", null, null, 2, 2, "NZ")]
    public async Task WhenUserSearchesWithSort(string scenario, string? sortBy, string? ascDesc, int? page, int? pageSize, string expectedOrder)
    {
        Debug.WriteLine(scenario);
        GivenTheseCountries(TestCountries);

        var result = await WhenSearchingForCountries(sortBy: sortBy, ascDesc: ascDesc, page: page, pageSize: pageSize);

        var codes = expectedOrder?.Split(",") ?? new string[] { };
        result.Count().Should().Be(codes.Count());
        if (codes.Any())
            result.Select(c => c.Code).Should().BeEquivalentTo(codes, options => options.WithStrictOrdering());
    }

    public static IEnumerable<object[]> CountriesByIdTestData()
    {
        yield return new object[] { "GB-123", HttpStatusCode.OK, CountryGBAsDTO };
        yield return new object[] { "FR-123", HttpStatusCode.OK, CountryFRAsDTO };
        yield return new object[] { "invalid-id", HttpStatusCode.NotFound, null! };
    }

    private void GivenTheseCountries(List<CountryDocument> countryList)
    {
        _countryRepoMock.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ReadOnlyCollection<CountryDocument>(countryList));
        foreach (var c in countryList)
        {
            _countryRepoMock.Setup(m => m.GetByIdAsync(c.IdentifierId, It.IsAny<CancellationToken>())).ReturnsAsync(c);
        }
    }

    private async Task<List<CountryDTO>> WhenSearchingForCountries(string? name = null, string? codeCsv = null, bool? euTradeMember = null, bool? devolvedAuthority = null, DateTime? lastUpdated = null, string? sortBy = null, string? ascDesc = null, int? page = null, int? pageSize = null)
    {
        var query = new GetCountriesQuery()
        {
            Code = codeCsv,
            Name = name,
            EuTradeMember = euTradeMember,
            DevolvedAuthority = devolvedAuthority,
            Sort = ascDesc,
            Order = sortBy,
            LastUpdatedDate = lastUpdated,
            Page = page ?? 1,
            PageSize = pageSize ?? 10
        };

        return (await _sut.GetCountriesAsync(query, CancellationToken.None)).Item1;
    }
}