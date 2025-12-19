using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Xunit.Sdk;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class CountriesEndpointTests : IClassFixture<AppTestFixture>
{
    private readonly HttpClient _client;
    private Mock<ICountryRepository> _countryRepoMock;

    public CountriesEndpointTests(AppTestFixture appTestFixture)
    {
        _countryRepoMock = appTestFixture.AppWebApplicationFactory._countriesRepositoryMock;
        _client = appTestFixture.HttpClient;
    }

    private static readonly DateTime GBLastUpdated = new DateTime(2012, 08, 18, 11, 10, 0);

    private List<CountryDocument> TestCountries = new List<CountryDocument> {
            new() { IdentifierId = "GB-123", Code = "GB", Name = "UK", LongName = "United Kingdom", IsActive = true, DevolvedAuthority = true, EuTradeMember = false, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.MinValue, LastModifiedDate = GBLastUpdated },
            new() { IdentifierId = "NZ-123", Code = "NZ", Name = "New Zealand", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = "FR-123", Code = "FR", Name = "France", LongName = "France", IsActive = true, SortOrder = 20, EuTradeMember = true, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
        };

    private static CountryDTO CountryGBAsDTO = new CountryDTO { Code = "GB", IdentifierId = "GB-123", Name = "UK", LongName = "United Kingdom", DevolvedAuthorityFlag = true, EuTradeMemberFlag = false, LastUpdatedDate = GBLastUpdated };
    private static CountryDTO CountryFRAsDTO = new CountryDTO { Code = "FR", IdentifierId = "FR-123", Name = "France", LongName = "France", DevolvedAuthorityFlag = false, EuTradeMemberFlag = true };

    [Fact]
    public async Task WhenEndpointHitWithNoParams_AllCountriesShouldBeReturned()
    {
        GivenTheseCountries(TestCountries);
        var result = await WhenPerformGetOnCountriesEndpoint(HttpStatusCode.OK);

        var expected = CountryGBAsDTO;

        result!.Values!.Count().Should().Be(3);
        result!.Values.Should().Contain(x => x.Code == "GB");
        var gb = result!.Values.Single(x => x.Code == "GB");
        gb.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("Search by Name", "France", null, null, null, HttpStatusCode.OK, "FR")]
    [InlineData("Search by Name that doesnt exist", "NotRealCountry", null, null, null, HttpStatusCode.OK, null)]
    [InlineData("Search by One country code", null, "NZ", null, null, HttpStatusCode.OK, "NZ")]
    [InlineData("Search by multiple country codes", null, "FR,NZ", null, null, HttpStatusCode.OK, "FR,NZ")]
    [InlineData("Search by isEutrademember(true)", null, null, true, null, HttpStatusCode.OK, "FR")]
    [InlineData("Search by isEutrademember(false)", null, null, false, null, HttpStatusCode.OK, "GB,NZ")]
    [InlineData("Search by isDevolvedAuthority(true)", null, null, null, true, HttpStatusCode.OK, "GB")]
    [InlineData("Search by isDevolvedAuthority(false)", null, null, null, false, HttpStatusCode.OK, "FR,NZ")]
    public async Task WhenUserSearchesAppropriateCountriesShouldBeReturned(string scenario, string? name, string? codesCsv, bool? euTradeMember, bool? devolvedAuthority, HttpStatusCode expectedHttpCode, string? expectedCodes)
    {
        Debug.WriteLine(scenario);
        GivenTheseCountries(TestCountries);

        var result = await WhenPerformGetOnCountriesEndpoint(expectedHttpCode, name, codesCsv, euTradeMember, devolvedAuthority);

        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var codes = expectedCodes?.Split(",") ?? new string[] { };
            result!.Values!.Count().Should().Be(codes.Count());
            if (codes.Any())
                result!.Values.Select(c => c.Code).Should().BeEquivalentTo(codes);
        }
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

        var result = await WhenPerformGetOnCountriesEndpoint(HttpStatusCode.OK, sortBy: sortBy, ascDesc: ascDesc, page: page, pageSize: pageSize);

        var codes = expectedOrder?.Split(",") ?? new string[] { };
        result!.Values!.Count().Should().Be(codes.Count());
        if (codes.Any())
            result!.Values.Select(c => c.Code).Should().BeEquivalentTo(codes, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task WhenSearchWithInvalidParameter()
    {
        GivenTheseCountries(TestCountries);

        var result = await WhenPerformGetOnCountriesEndpoint(HttpStatusCode.BadRequest, sortBy: "invalidField");
    }

    public static IEnumerable<object[]> CountriesByIdTestData()
    {
        yield return new object[] { "GB-123", HttpStatusCode.OK, CountryGBAsDTO };
        yield return new object[] { "FR-123", HttpStatusCode.OK, CountryFRAsDTO };
        yield return new object[] { "invalid-id", HttpStatusCode.NotFound, null! };
    }

    [Theory]
    [MemberData(nameof(CountriesByIdTestData))]
    public async Task GetCountryByIdShouldReturnCorrectRecord(string id, HttpStatusCode expectedHttpCode, CountryDTO? expectedRecord)
    {
        GivenTheseCountries(TestCountries);

        var response = await WhenPerformGetOnCountryByIdEndpoint(expectedHttpCode, id);

        response.StatusCode.Should().Be(expectedHttpCode);

        if (expectedRecord != null)
        {
            var result = await response.Content.ReadFromJsonAsync<CountryDTO>();
            if (result == null)
                Assert.Fail("content not readable");

            result.Should().BeEquivalentTo(expectedRecord);
        }
    }

    private void GivenTheseCountries(List<CountryDocument> countryList)
    {
        _countryRepoMock.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ReadOnlyCollection<CountryDocument>(countryList));
        foreach (var c in countryList)
        {
            _countryRepoMock.Setup(m => m.GetByIdAsync(c.IdentifierId, It.IsAny<CancellationToken>())).ReturnsAsync(c);
        }
    }

    private async Task<PaginatedResult<CountryDTO>?> WhenPerformGetOnCountriesEndpoint(HttpStatusCode expectedHttpCode, string? name = null, string? codeCsv = null, bool? euTradeMember = null, bool? devolvedAuthority = null, string? sortBy = null, string? ascDesc = null, int? page = null, int? pageSize = null)
    {
        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (name != null)
            queryString.Add("name", name);

        if (codeCsv != null)
            queryString.Add("code", codeCsv);

        if (euTradeMember.HasValue)
            queryString.Add("euTradeMember", euTradeMember.Value.ToString());

        if (devolvedAuthority.HasValue)
            queryString.Add("devolvedAuthority", devolvedAuthority.Value.ToString());

        if (sortBy != null)
            queryString.Add("order", sortBy);

        if (ascDesc != null)
            queryString.Add("sort", ascDesc);

        if (page != null)
            queryString.Add("page", page.ToString());

        if (pageSize != null)
            queryString.Add("pageSize", pageSize.ToString());

        var query = queryString.ToString();

        var response = await _client.GetAsync("api/countries?" + query);

        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<CountryDTO>>();
            if (result == null)
                Assert.Fail("content not readable as paginated response");

            return result;
        }
        return null;
    }

    private async Task<HttpResponseMessage> WhenPerformGetOnCountryByIdEndpoint(HttpStatusCode expectedHttpCode, string id)
    {
        return await _client.GetAsync($"api/countries/{id}");
    }
}