using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
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

    [Fact]
    public async Task WhenEndpointHitWithNoParamsAllCountriesShouldBeReturned()
    {
        var countryList = new List<CountryDocument> {
            new() { IdentifierId = "GB", Code = "GB", Name = "United Kingdom", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = "FR", Code = "FR", Name = "France", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        GivenTheseCountries(countryList);
        var result = await WhenPerformGetOnCountriesEndpoint(HttpStatusCode.OK);

        // then all countries returned with format
        result?.Values.Count().Should().Be(2);
        Assert.Fail("return in correct format");
    }

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


    private async Task<PaginatedResult<CountrySummaryDocument>?> WhenPerformGetOnCountriesEndpoint(HttpStatusCode expectedHttpCode)
    {
        var response = await _client.GetAsync("api/countries");
        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<PaginatedResult<CountrySummaryDocument>>();
            if (result == null)
                Assert.Fail("content not readable as paginated response");

            return result;
        }
        return null;
    }
}