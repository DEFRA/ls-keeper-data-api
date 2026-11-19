using KeeperData.Api.Tests.Component.Authentication.Fakes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Api.Tests.Component;

public class AppWebApplicationFactory(
    IDictionary<string, string?>? configurationOverrides = null,
    bool useFakeAuth = false) : WebApplicationFactory<Program>
{
    public Mock<IMongoClient>? MongoClientMock;
    public readonly Mock<HttpMessageHandler> DataBridgeApiClientHttpMessageHandlerMock = new();

    public readonly Mock<ISitesRepository> _sitesRepositoryMock = new();
    public readonly Mock<IPartiesRepository> _partiesRepositoryMock = new();
    public readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    public readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    public readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    public readonly Mock<ICountryRepository> _countryRepositoryMock = new();
    public readonly Mock<IUserAccountsRepository> _userAccountsRepositoryMock = new();

    public readonly Mock<IReferenceDataCache> _referenceDataCacheMock = new();

    private readonly List<Action<IServiceCollection>> _overrideServices = [];
    private readonly IDictionary<string, string?> _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
    private readonly bool _useFakeAuth = useFakeAuth;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.ApplicationKey, typeof(Program).Assembly.FullName);

        SetTestEnvironmentVariables();

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            if (_configurationOverrides.Count > 0)
                configBuilder.AddInMemoryCollection(_configurationOverrides);
        });

        builder.ConfigureTestServices(services =>
        {
            RemoveService<IHealthCheckPublisher>(services);

            ConfigureRepositories();
            ConfigureReferenceDataCache(services);
            ConfigureDatabase(services);

            services.AddHttpClient("DataBridgeApi")
                .ConfigurePrimaryHttpMessageHandler(() => DataBridgeApiClientHttpMessageHandlerMock.Object);

            if (_useFakeAuth)
            {
                ConfigureFakeAuthorization(services);
            }

            foreach (var applyOverride in _overrideServices)
            {
                applyOverride(services);
            }

            services.RemoveAll<IHostedService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (_configurationOverrides.Count > 0)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(_configurationOverrides);
            });
        }

        return base.CreateHost(builder);
    }

    public T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }

    public void OverrideServiceAsSingleton<T>(T implementation) where T : class
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddSingleton(implementation);
        });
    }

    public void OverrideServiceAsTransient<T, TH>()
        where T : class
        where TH : class, T
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddTransient<T, TH>();
        });
    }

    public void OverrideServiceAsTransient<T>(T instance)
        where T : class
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddTransient(_ => instance);
        });
    }

    public void OverrideServiceAsScoped<T>(T implementation) where T : class
    {
        _overrideServices.Add(services =>
        {
            services.RemoveAll<T>();
            services.AddScoped(_ => implementation);
        });
    }

    public void ResetMocks()
    {
        MongoClientMock!.Reset();
        DataBridgeApiClientHttpMessageHandlerMock.Reset();
        ResetRepositoryMocks();
        ResetReferenceDataCache();
    }

    private void ResetReferenceDataCache()
    {
        _referenceDataCacheMock.Reset();
    }

    private static void SetTestEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("Mongo__DatabaseUri", "mongodb://localhost:27017");
        Environment.SetEnvironmentVariable("Mongo__DatabaseName", "test-keeper-data-api");
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__HealthcheckEnabled", "true");
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__BaseUrl", TestConstants.DataBridgeApiBaseUrl);
        Environment.SetEnvironmentVariable("ApiClients__DataBridgeApi__BridgeApiSubscriptionKey", "XYZ");
        Environment.SetEnvironmentVariable("LegacyEndpointsEnabled", "true");
        Environment.SetEnvironmentVariable("AuthenticationConfiguration__EnableApiKey", "true");
        Environment.SetEnvironmentVariable("AuthenticationConfiguration__ApiGatewayExists", "true");
        Environment.SetEnvironmentVariable("AuthenticationConfiguration__Authority", "https://fake-authority/");
    }

    private static void ConfigureFakeAuthorization(IServiceCollection services)
    {
        services.RemoveAll<IConfigureNamedOptions<JwtBearerOptions>>();

        services.RemoveAll<IAuthenticationSchemeProvider>();

        services.AddSingleton<IAuthenticationSchemeProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AuthenticationOptions>>();
            var provider = new AuthenticationSchemeProvider(options);

            provider.RemoveScheme("Bearer");
            provider.AddScheme(new AuthenticationScheme(
                FakeJwtHandler.SchemeName,
                FakeJwtHandler.SchemeName,
                typeof(FakeJwtHandler)));

            return provider;
        });
    }

    private void ConfigureReferenceDataCache(IServiceCollection services)
    {
        // Configure Reference Data Cache Mock
        _referenceDataCacheMock.Setup(c => c.SiteTypeMaps).Returns(new List<SiteTypeMapDocument>());
        _referenceDataCacheMock.Setup(c => c.Countries).Returns(new List<CountryDocument>());
        _referenceDataCacheMock.Setup(c => c.Species).Returns(new List<SpeciesDocument>());
        _referenceDataCacheMock.Setup(c => c.Roles).Returns(new List<RoleDocument>());
        _referenceDataCacheMock.Setup(c => c.SiteTypes).Returns(new List<SiteTypeDocument>());
        _referenceDataCacheMock.Setup(c => c.SiteActivityTypes).Returns(new List<SiteActivityTypeDocument>());
        _referenceDataCacheMock.Setup(c => c.SiteIdentifierTypes).Returns(new List<SiteIdentifierTypeDocument>());
        _referenceDataCacheMock.Setup(c => c.ProductionUsages).Returns(new List<ProductionUsageDocument>());
        _referenceDataCacheMock.Setup(c => c.ActivityMaps).Returns(new List<FacilityBusinessActivityMapDocument>());
    }

    private void ConfigureRepositories()
    {
        OverrideServiceAsScoped(_sitesRepositoryMock.Object);
        OverrideServiceAsScoped(_partiesRepositoryMock.Object);

        OverrideServiceAsScoped(_goldSiteRepositoryMock.Object);
        OverrideServiceAsScoped(_goldPartyRepositoryMock.Object);

        OverrideServiceAsScoped(_roleRepositoryMock.Object);
        OverrideServiceAsScoped(_countryRepositoryMock.Object);

        OverrideServiceAsScoped(_userAccountsRepositoryMock.Object);

        OverrideServiceAsSingleton(_referenceDataCacheMock.Object);

        ConfigureDefaultRepositoryBehavior();
    }

    private void ConfigureDefaultRepositoryBehavior()
    {
        _partiesRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartyDocument>());

        _partiesRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    private void ResetRepositoryMocks()
    {
        _sitesRepositoryMock.Reset();
        _partiesRepositoryMock.Reset();

        _goldSiteRepositoryMock.Reset();
        _goldPartyRepositoryMock.Reset();

        _roleRepositoryMock.Reset();
        _countryRepositoryMock.Reset();

        _userAccountsRepositoryMock.Reset();
    }

    private void ConfigureDatabase(IServiceCollection services)
    {
        var mongoDatabaseMock = new Mock<IMongoDatabase>();
        var mongoCollectionMock = new Mock<IMongoCollection<BsonDocument>>();
        var indexManagerMock = new Mock<IMongoIndexManager<BsonDocument>>();

        indexManagerMock
            .Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<BsonDocument>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        indexManagerMock
            .Setup(x => x.CreateManyAsync(It.IsAny<IClientSessionHandle>(), It.IsAny<IEnumerable<CreateIndexModel<BsonDocument>>>(),
                It.IsAny<CreateManyIndexesOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        mongoCollectionMock
            .SetupGet(x => x.Indexes)
            .Returns(indexManagerMock.Object);

        indexManagerMock
            .Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmptyCursor());

        mongoDatabaseMock
            .Setup(x => x.GetCollection<BsonDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(mongoCollectionMock.Object);

        MongoClientMock = new Mock<IMongoClient>();

        MongoClientMock.Setup(x => x.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(mongoDatabaseMock.Object);

        services.Replace(new ServiceDescriptor(typeof(IMongoClient), MongoClientMock.Object));
    }

    private static IAsyncCursor<BsonDocument> CreateEmptyCursor()
    {
        var mockCursor = new Mock<IAsyncCursor<BsonDocument>>();

        mockCursor.Setup(x => x.MoveNext(It.IsAny<CancellationToken>()))
                  .Returns(false);

        mockCursor.Setup(x => x.MoveNextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        mockCursor.SetupGet(x => x.Current)
                  .Returns([]);

        return mockCursor.Object;
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        services.RemoveAll(typeof(T));
    }
}