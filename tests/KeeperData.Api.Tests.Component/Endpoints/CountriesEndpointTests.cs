using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class CountriesEndpointTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _fixture;
    private readonly HttpClient _client;


    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<CountryListDocument>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<CountryListDocument>> _mongoCollectionMock = new();
    private readonly CountryRepository _countryRepo;

    public CountriesEndpointTests(AppTestFixture fixture)
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "TestDatabase" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<CountryListDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_mongoCollectionMock.Object);

        _mongoClientMock
            .Setup(client => client.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(_mongoDatabaseMock.Object);

        _asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mongoCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<CountryListDocument>>(),
                It.IsAny<FindOptions<CountryListDocument, CountryListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session)
            .Returns(_clientSessionHandleMock.Object);

        _countryRepo = new CountryRepository(_mongoConfig, _mongoClientMock.Object, _unitOfWorkMock.Object);

        typeof(GenericRepository<CountryListDocument>)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_countryRepo, _mongoCollectionMock.Object);


        _fixture = fixture;

        _client = fixture.AppWebApplicationFactory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICountryRepository>();
                    services.AddScoped(_ => (ICountryRepository)_countryRepo);
                });
            })
            .CreateClient();
    }

    private static readonly DateTime GBLastUpdated = new DateTime(2012,08,18,11,10,0);


    private List<CountryDocument> TestCountries = new List<CountryDocument> {
            new() { IdentifierId = "GB-123", Code = "GB", Name = "UK", LongName = "United Kingdom", IsActive = true, DevolvedAuthority = true, EuTradeMember = false, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.MinValue, LastModifiedDate = GBLastUpdated },
            new() { IdentifierId = "FR-123", Code = "FR", Name = "France", IsActive = true, SortOrder = 20, EuTradeMember = true, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = "NZ-123", Code = "NZ", Name = "New Zealand", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

    [Fact]
    public async Task WhenEndpointHitWithNoParams_AllCountriesShouldBeReturned()
    {
        GivenTheseCountries(TestCountries);
        var result = await WhenPerformGetOnCountriesEndpoint(HttpStatusCode.OK);

        var expected = new CountryDTO { Code = "GB", IdentifierId = "GB-123", Name = "UK", LongName = "United Kingdom", DevolvedAuthorityFlag = true, EuTradeMemberFlag = false, LastUpdatedDate = GBLastUpdated };

        result!.Values!.Count().Should().Be(3);
        result!.Values.Should().Contain(x => x.Code == "GB");
        var gb = result!.Values.Single(x => x.Code == "GB");
        gb.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("France", null, HttpStatusCode.OK, "FR-123")]
    [InlineData(null, "FR-123,NZ-123", HttpStatusCode.OK, "FR-123,NZ-123")]
    [InlineData("NotRealCountry", null, HttpStatusCode.OK, "")]
    public async Task WhenUserSearchesAppropriateCountriesShouldBeReturned(string name, string codesCsv, HttpStatusCode expectedHttpCode, string expectedCodes)
    {
        GivenTheseCountries(TestCountries);

        var result = await WhenPerformGetOnCountriesEndpoint(expectedHttpCode, name, codesCsv);

        if (expectedHttpCode == HttpStatusCode.OK){
            var codes = expectedCodes.Split(",");
            result!.Values!.Count().Should().Be(codes.Count());
            if (codes.Any())
                result!.Values.Select(c => c.Code).Should().BeEquivalentTo(expectedCodes);
        }
    }

    /*
    Given : User wants to search for a country based on name
When : User hits endpoint with  incorrect name
Then : User see Country not found message with 404 code.
 
Given : User wants to search for a country based on name
When : User hits endpoint with  incorrect parameter
Then : User see 400 invalid request message.
 
Given : User wants to search for a country based on code
When : User hits endpoint with  correct code
Then : User see correct information returned with 200 code.
 
Given : User wants to search for a list of countries based on code
When : User hits endpoint with  correct codes.
Then : User see list of countries matching entered codes.
 
Given : User wants to search for a country based on euTradeMember flag
When : User hits endpoint with  flag set to true.
Then : User see list of EU countries.
 
Given : User wants to search for a country based on euTradeMember flag
When : User hits endpoint with  flag set to false.
Then : User see list of non- EU countries.
 
Given : User wants to search for a country based on devolvedAuthority flag
When : User hits endpoint with  flag set to true.
Then : User see list of devolved authority.
 
Given : User wants to search for a country based on euTradeMember flag
When : User hits endpoint with  flag set to false.
Then : User see list of non devolved authority.
 
Given : User wants to search for first 59 records.
When : User enters page : 0 and  page size : 59
Then : First 59 records are displayed.
 
Given : User wants to search for subsequent 59 records.
When : User enters page : 1 and  page size : 59
Then : Subsequent 59 records are displayed.
 
Given : User wants to search records in  alphabetical order.
When : User enters order: name  and sort : asc
Then : List of countries are displayed in alphabetical order.

*/



    private void GivenTheseCountries(List<CountryDocument> countryList)
    {
        var listDocument = new CountryListDocument
        {
            Id = "all-countries",
            Countries = countryList
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);
    }

    /*
        [Theory]
        [InlineData("England", 1)]
        public async Task WhenEndpointHitWithSearvhByName_NamedCountryShouldBeReturned()
        {
            var result = await WhenPerformGetOnCountriesEndpoint(HttpStatusCode.OK);

            // then all countries returned with format
            //result.Count().Should().Be(249);
            Assert.Fail("todo - return all");
        }*/


    private async Task<PaginatedResult<CountryDTO>?> WhenPerformGetOnCountriesEndpoint(HttpStatusCode expectedHttpCode, string? name = null, string? codeCsv = null)
    {
        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (name != null)
            queryString.Add("name", name);

        if (codeCsv != null)
            queryString.Add("code", codeCsv);
            
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
}