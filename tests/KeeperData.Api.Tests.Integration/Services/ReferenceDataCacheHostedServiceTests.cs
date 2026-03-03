using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using KeeperData.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;

namespace KeeperData.Api.Tests.Integration.Services;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class ReferenceDataCacheHostedServiceTests : IClassFixture<MongoDbFixture>
{
    private readonly MongoDbFixture _fixture;
    private readonly IMongoDatabase _database;

    private const string ReferenceDataCollection = "referenceData";

    private readonly CountryDocument _testCountry = new()
    {
        IdentifierId = "country-1",
        Code = "GB",
        Name = "United Kingdom",
        IsActive = true
    };

    public ReferenceDataCacheHostedServiceTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _database = _fixture.MongoClient.GetDatabase(MongoDbFixture.KrdsDatabaseName);
    }

    [Fact]
    public async Task HostedService_StartAsync_ShouldInitializeCache()
    {
        // Arrange
        await SeedCountryDataAsync();
        var host = CreateHostWithCache();

        // Act - Start the host which triggers StartAsync on all hosted services
        await host.StartAsync();

        // Assert
        var cache = host.Services.GetRequiredService<IReferenceDataCache>();
        cache.Countries.Should().HaveCount(1);
        cache.Countries.First().Code.Should().Be("GB");

        // Cleanup
        await host.StopAsync();
        host.Dispose();
        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task HostedService_StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        await SeedCountryDataAsync();
        var host = CreateHostWithCache();
        await host.StartAsync();

        // Act
        var stopAction = async () => await host.StopAsync();

        // Assert
        await stopAction.Should().NotThrowAsync();

        // Cleanup
        host.Dispose();
        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task HostedService_RegisteredAsSingletonAndHostedService_ShouldBeSameInstance()
    {
        // Arrange
        await SeedCountryDataAsync();
        var host = CreateHostWithCache();
        await host.StartAsync();

        // Act
        var cacheFromInterface = host.Services.GetRequiredService<IReferenceDataCache>();
        var hostedServices = host.Services.GetServices<IHostedService>();
        var cacheAsHostedService = hostedServices.OfType<ReferenceDataCache>().FirstOrDefault();

        // Assert
        cacheAsHostedService.Should().NotBeNull();
        ReferenceEquals(cacheFromInterface, cacheAsHostedService).Should().BeTrue();

        // Cleanup
        await host.StopAsync();
        host.Dispose();
        await CleanupReferenceDataAsync();
    }

    [Fact]
    public async Task HostedService_InitializedOnStart_CacheIsAccessibleImmediately()
    {
        // Arrange
        await SeedCountryDataAsync();
        var host = CreateHostWithCache();

        // Act
        await host.StartAsync();

        // Assert - Cache should be usable immediately after host starts
        var cache = host.Services.GetRequiredService<IReferenceDataCache>();
        cache.Countries.Should().NotBeEmpty();

        // Cleanup
        await host.StopAsync();
        host.Dispose();
        await CleanupReferenceDataAsync();
    }

    private IHost CreateHostWithCache()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
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

                services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

                // Register exactly as in Program.cs
                services.AddSingleton<IReferenceDataCache, ReferenceDataCache>();
                services.AddHostedService(sp => (ReferenceDataCache)sp.GetRequiredService<IReferenceDataCache>());
            });

        return hostBuilder.Build();
    }

    private async Task SeedCountryDataAsync()
    {
        var collection = _database.GetCollection<BsonDocument>(ReferenceDataCollection);
        var itemsBsonArray = new BsonArray { _testCountry.ToBsonDocument() };

        var document = new BsonDocument
        {
            { "_id", CountryListDocument.DocumentId },
            { "lastUpdatedDate", BsonValue.Create(DateTime.UtcNow) },
            { "countries", itemsBsonArray }
        };

        await collection.InsertOneAsync(document);
    }

    private async Task CleanupReferenceDataAsync()
    {
        var collection = _database.GetCollection<BsonDocument>(ReferenceDataCollection);
        var filter = Builders<BsonDocument>.Filter.Empty;
        await collection.DeleteManyAsync(filter);
    }
}