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

public class CountriesEndpointTests
{
    private readonly HttpClient _client;
    private Mock<ICountryRepository> _countryRepoMock;

    public CountriesEndpointTests()
    {
        _countryRepoMock = new Mock<ICountryRepository>();
        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_countryRepoMock.Object);
        _client = factory.CreateClient();
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

    private async Task<PaginatedResult<CountryDTO>?> WhenPerformGetOnCountriesEndpoint(HttpStatusCode expectedHttpCode)
    {
        var response = await _client.GetAsync("api/countries");

        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<CountryDTO>>();
            if (result == null)
                Assert.Fail("content not readable as paginated response");

            return result;
        }
        return null;
    }

    private void GivenTheseCountries(List<CountryDocument> countryList)
    {
        _countryRepoMock.Setup(c => c.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ReadOnlyCollection<CountryDocument>(countryList));
    }
}