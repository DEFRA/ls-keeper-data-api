using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Services;
using KeeperData.Api.Tests.Integration.Fixtures;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Services;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class ReferenceDataCacheTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly IMongoDatabase _database;
    private ServiceProvider _serviceProvider = null!;

    private const string ReferenceDataCollection = "referenceData";

    private readonly CountryDocument _testCountry = new()
    {
        IdentifierId = "country-1",
        Code = "GB",
        Name = "United Kingdom",
        LongName = "United Kingdom of Great Britain and Northern Ireland",
        IsActive = true
    };

    private readonly CountryDocument _testCountry2 = new()
    {
        IdentifierId = "country-2",
        Code = "IE",
        Name = "Ireland",
        LongName = "Ireland",
        IsActive = true
    };

    private readonly SpeciesDocument _testSpecies = new()
    {
        IdentifierId = "species-1",
        Code = "CATTLE",
        Name = "Cattle",
        IsActive = true
    };

    private readonly RoleDocument _testRole = new()
    {
        IdentifierId = "role-1",
        Code = "KEEPER",
        Name = "Keeper",
        IsActive = true
    };

    private readonly SiteTypeDocument _testSiteType = new()
    {
        IdentifierId = "pt-1",
        Code = "FARM",
        Name = "Farm",
        IsActive = true
    };

    private readonly SiteActivityTypeDocument _testSiteActivityType = new()
    {
        IdentifierId = "pat-1",
        Code = "KEEPING",
        Name = "Keeping",
        IsActive = true
    };

    private readonly SiteIdentifierTypeDocument _testSiteIdentifierType = new()
    {
        IdentifierId = "sit-1",
        Code = "CPH",
        Name = "County Parish Holding",
        IsActive = true
    };

    private readonly ProductionUsageDocument _testProductionUsage = new()
    {
        IdentifierId = "pu-1",
        Code = "MEAT",
        Description = "Meat production",
        IsActive = true
    };

    private readonly FacilityBusinessActivityMapDocument _testActivityMap = new()
    {
        IdentifierId = "fba-1",
        FacilityActivityCode = "FA01",
        AssociatedSiteTypeCode = "FARM",
        AssociatedSiteActivityCode = "KEEPING",
        IsActive = true
    };

    public ReferenceDataCacheTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _database = _fixture.MongoClient.GetDatabase(MongoDbFixture.KrdsDatabaseName);
        InitializeServiceProvider();
    }

    private void InitializeServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_fixture.MongoClient);
        services.AddSingleton(Options.Create(new MongoConfig
        {
            DatabaseUri = _fixture.ConnectionString!,
            DatabaseName = MongoDbFixture.KrdsDatabaseName
        }));
        var unitOfWorkMock = Substitute.For<IUnitOfWork>();
        services.AddScoped<IUnitOfWork>(_ => unitOfWorkMock);
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IFacilityBusinessActivityMapRepository, FacilityBusinessActivityMapRepository>();
        services.AddScoped<IProductionUsageRepository, ProductionUsageRepository>();
        services.AddScoped<ISiteIdentifierTypeRepository, SiteIdentifierTypeRepository>();
        services.AddLogging(b => b.AddConsole());

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task InitializeAsync_WithSeededData_LoadsAllReferenceDataTypes()
    {
        await SeedAllReferenceDataAsync();

        var cache = CreateCache();

        await cache.InitializeAsync();

        cache.Countries.Should().HaveCount(2);
        cache.Species.Should().HaveCount(1);
        cache.Roles.Should().HaveCount(1);
        cache.SiteTypes.Should().HaveCount(1);
        cache.SiteActivityTypes.Should().HaveCount(1);
        cache.SiteIdentifierTypes.Should().HaveCount(1);
        cache.ProductionUsages.Should().HaveCount(1);
        cache.ActivityMaps.Should().HaveCount(1);

        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task InitializeAsync_WithSeededData_ReturnsCorrectDocumentValues()
    {
        await SeedAllReferenceDataAsync();

        var cache = CreateCache();

        await cache.InitializeAsync();

        var gb = cache.Countries.FirstOrDefault(c => c.Code == "GB");
        gb.Should().NotBeNull();
        gb!.Name.Should().Be("United Kingdom");
        gb.IdentifierId.Should().Be("country-1");

        var ie = cache.Countries.FirstOrDefault(c => c.Code == "IE");
        ie.Should().NotBeNull();
        ie!.Name.Should().Be("Ireland");

        var cattle = cache.Species.FirstOrDefault(s => s.Code == "CATTLE");
        cattle.Should().NotBeNull();
        cattle!.Name.Should().Be("Cattle");

        var keeper = cache.Roles.FirstOrDefault(r => r.Code == "KEEPER");
        keeper.Should().NotBeNull();
        keeper!.Name.Should().Be("Keeper");

        var activityMap = cache.ActivityMaps.FirstOrDefault(a => a.FacilityActivityCode == "FA01");
        activityMap.Should().NotBeNull();
        activityMap!.AssociatedSiteTypeCode.Should().Be("FARM");
        activityMap.AssociatedSiteActivityCode.Should().Be("KEEPING");

        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyCollections_ReturnsEmptyNotNull()
    {
        var cache = CreateCache();

        await cache.InitializeAsync();

        cache.Countries.Should().NotBeNull().And.BeEmpty();
        cache.Species.Should().NotBeNull().And.BeEmpty();
        cache.Roles.Should().NotBeNull().And.BeEmpty();
        cache.SiteTypes.Should().NotBeNull().And.BeEmpty();
        cache.SiteActivityTypes.Should().NotBeNull().And.BeEmpty();
        cache.SiteIdentifierTypes.Should().NotBeNull().And.BeEmpty();
        cache.ProductionUsages.Should().NotBeNull().And.BeEmpty();
        cache.ActivityMaps.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_WithPartialData_LoadsOnlySeededTypes()
    {
        var collection = _database.GetCollection<BsonDocument>(ReferenceDataCollection);
        await SeedReferenceListAsync(collection, CountryListDocument.DocumentId, "countries",
            new[] { _testCountry });

        var cache = CreateCache();

        await cache.InitializeAsync();

        cache.Countries.Should().HaveCount(1);
        cache.Species.Should().BeEmpty();
        cache.Roles.Should().BeEmpty();
        cache.SiteTypes.Should().BeEmpty();
        cache.SiteActivityTypes.Should().BeEmpty();
        cache.SiteIdentifierTypes.Should().BeEmpty();
        cache.ProductionUsages.Should().BeEmpty();
        cache.ActivityMaps.Should().BeEmpty();

        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_ReloadsData()
    {
        await SeedAllReferenceDataAsync();

        var cache = CreateCache();

        await cache.InitializeAsync();
        cache.Countries.Should().HaveCount(2);

        var collection = _database.GetCollection<BsonDocument>(ReferenceDataCollection);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", CountryListDocument.DocumentId);
        await collection.DeleteOneAsync(filter);

        var singleCountryDoc = new CountryListDocument
        {
            Id = CountryListDocument.DocumentId,
            LastUpdatedDate = DateTime.UtcNow,
            Countries = [_testCountry]
        };
        await collection.InsertOneAsync(singleCountryDoc.ToBsonDocument());

        await cache.InitializeAsync();
        cache.Countries.Should().HaveCount(1);

        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task StartAsync_AsHostedService_PopulatesCache()
    {
        await SeedAllReferenceDataAsync();

        var cache = CreateCache();

        await ((IHostedService)cache).StartAsync(CancellationToken.None);

        cache.Countries.Should().NotBeEmpty();
        cache.Species.Should().NotBeEmpty();
        cache.Roles.Should().NotBeEmpty();

        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task InitializeAsync_LoadsDataConcurrently_AllTypesPopulated()
    {
        await SeedAllReferenceDataAsync();

        var cache = CreateCache();

        await cache.InitializeAsync();

        var allTypes = new object[]
        {
            cache.Countries,
            cache.Species,
            cache.Roles,
            cache.SiteTypes,
            cache.SiteActivityTypes,
            cache.SiteIdentifierTypes,
            cache.ProductionUsages,
            cache.ActivityMaps
        };

        allTypes.Should().AllSatisfy(t =>
        {
            var collection = (System.Collections.ICollection)t;
            collection.Count.Should().BeGreaterThan(0);
        });

        await CleanupReferenceDataAsync();
    }

    private ReferenceDataCache CreateCache()
    {
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = _serviceProvider.GetRequiredService<ILogger<ReferenceDataCache>>();
        return new ReferenceDataCache(scopeFactory, logger);
    }

    private async Task SeedAllReferenceDataAsync()
    {
        var collection = _database.GetCollection<BsonDocument>(ReferenceDataCollection);

        await Task.WhenAll(
            SeedReferenceListAsync(collection, CountryListDocument.DocumentId, "countries",
                new[] { _testCountry, _testCountry2 }),
            SeedReferenceListAsync(collection, SpeciesListDocument.DocumentId, "species",
                new[] { _testSpecies }),
            SeedReferenceListAsync(collection, RoleListDocument.DocumentId, "roles",
                new[] { _testRole }),
            SeedReferenceListAsync(collection, SiteTypeListDocument.DocumentId, "siteTypes",
                new[] { _testSiteType }),
            SeedReferenceListAsync(collection, SiteActivityTypeListDocument.DocumentId, "siteActivityTypes",
                new[] { _testSiteActivityType }),
            SeedReferenceListAsync(collection, SiteIdentifierTypeListDocument.DocumentId, "siteIdentifierTypes",
                new[] { _testSiteIdentifierType }),
            SeedReferenceListAsync(collection, ProductionUsageListDocument.DocumentId, "productionUsages",
                new[] { _testProductionUsage }),
            SeedReferenceListAsync(collection, FacilityBusinessActivityMapListDocument.DocumentId, "facilityBusinessActivityMaps",
                new[] { _testActivityMap })
        );
    }

    private async Task CleanupReferenceDataAsync()
    {
        var collection = _database.GetCollection<BsonDocument>(ReferenceDataCollection);
        var filter = Builders<BsonDocument>.Filter.Empty;
        await collection.DeleteManyAsync(filter);
    }

    private static async Task SeedReferenceListAsync<T>(
        IMongoCollection<BsonDocument> collection,
        string documentId,
        string itemsFieldName,
        T[] items)
    {
        var itemsBsonArray = new BsonArray(items.Select(i => i.ToBsonDocument()));

        var document = new BsonDocument
        {
            { "_id", documentId },
            { "lastUpdatedDate", BsonValue.Create(DateTime.UtcNow) },
            { itemsFieldName, itemsBsonArray }
        };

        await collection.InsertOneAsync(document);
    }
}