using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Services;

public class ReferenceDataCacheTests
{
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
    private readonly Mock<IServiceScope> _serviceScope;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<ILogger<ReferenceDataCache>> _logger;
    private readonly Mock<ICountryRepository> _countryRepository;
    private readonly Mock<ISpeciesRepository> _speciesRepository;
    private readonly Mock<IRoleRepository> _roleRepository;
    private readonly Mock<ISiteIdentifierTypeRepository> _siteIdentifierTypeRepository;
    private readonly Mock<IProductionUsageRepository> _productionUsageRepository;
    private readonly Mock<IFacilityBusinessActivityMapRepository> _activityMapRepository;
    private readonly Mock<IGenericRepository<SiteTypeListDocument>> _siteTypeListRepository;
    private readonly Mock<IGenericRepository<SiteActivityTypeListDocument>> _siteActivityTypeListRepository;
    private readonly Mock<IGenericRepository<SiteTypeMapListDocument>> _siteTypeMapListRepository;

    public ReferenceDataCacheTests()
    {
        _serviceScopeFactory = new Mock<IServiceScopeFactory>();
        _serviceScope = new Mock<IServiceScope>();
        _serviceProvider = new Mock<IServiceProvider>();
        _logger = new Mock<ILogger<ReferenceDataCache>>();

        _countryRepository = new Mock<ICountryRepository>();
        _speciesRepository = new Mock<ISpeciesRepository>();
        _roleRepository = new Mock<IRoleRepository>();
        _siteIdentifierTypeRepository = new Mock<ISiteIdentifierTypeRepository>();
        _productionUsageRepository = new Mock<IProductionUsageRepository>();
        _activityMapRepository = new Mock<IFacilityBusinessActivityMapRepository>();
        _siteTypeListRepository = new Mock<IGenericRepository<SiteTypeListDocument>>();
        _siteActivityTypeListRepository = new Mock<IGenericRepository<SiteActivityTypeListDocument>>();
        _siteTypeMapListRepository = new Mock<IGenericRepository<SiteTypeMapListDocument>>();

        _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(_serviceScope.Object);
        _serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);

        _serviceProvider.Setup(x => x.GetService(typeof(ICountryRepository))).Returns(_countryRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(ISpeciesRepository))).Returns(_speciesRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IRoleRepository))).Returns(_roleRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(ISiteIdentifierTypeRepository))).Returns(_siteIdentifierTypeRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IProductionUsageRepository))).Returns(_productionUsageRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IFacilityBusinessActivityMapRepository))).Returns(_activityMapRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IGenericRepository<SiteTypeListDocument>))).Returns(_siteTypeListRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IGenericRepository<SiteActivityTypeListDocument>))).Returns(_siteActivityTypeListRepository.Object);
        _serviceProvider.Setup(x => x.GetService(typeof(IGenericRepository<SiteTypeMapListDocument>))).Returns(_siteTypeMapListRepository.Object);
    }

    [Fact]
    public async Task InitializeAsync_WithValidData_LoadsAllReferenceDataTypes()
    {
        // Arrange
        var countries = new List<CountryDocument>
        {
            new() { IdentifierId = "c1", Code = "GB", Name = "United Kingdom", IsActive = true }
        };
        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = "s1", Code = "CATTLE", Name = "Cattle", IsActive = true }
        };
        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = "r1", Code = "KEEPER", Name = "Keeper", IsActive = true }
        };
        var siteIdentifierTypes = new List<SiteIdentifierTypeDocument>
        {
            new() { IdentifierId = "sit1", Code = "CPH", Name = "County Parish Holding", IsActive = true }
        };
        var productionUsages = new List<ProductionUsageDocument>
        {
            new() { IdentifierId = "pu1", Code = "MEAT", Description = "Meat production", IsActive = true }
        };
        var activityMaps = new List<FacilityBusinessActivityMapDocument>
        {
            new() { IdentifierId = "fba1", FacilityActivityCode = "FA01", IsActive = true }
        };
        var siteTypeList = new SiteTypeListDocument
        {
            Id = SiteTypeListDocument.DocumentId,
            SiteTypes = new List<SiteTypeDocument>
            {
                new() { IdentifierId = "pt1", Code = "FARM", Name = "Farm", IsActive = true }
            }
        };
        var siteActivityTypeList = new SiteActivityTypeListDocument
        {
            Id = SiteActivityTypeListDocument.DocumentId,
            SiteActivityTypes = new List<SiteActivityTypeDocument>
            {
                new() { IdentifierId = "pat1", Code = "KEEPING", Name = "Keeping", IsActive = true }
            }
        };

        _countryRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(countries);
        _speciesRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(species);
        _roleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);
        _siteIdentifierTypeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(siteIdentifierTypes);
        _productionUsageRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(productionUsages);
        _activityMapRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(activityMaps);
        _siteTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(siteTypeList);
        _siteActivityTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteActivityTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(siteActivityTypeList);
        _siteTypeMapListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeMapListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteTypeMapListDocument?)null);

        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        await cache.InitializeAsync();

        // Assert
        cache.Countries.Should().HaveCount(1);
        cache.Countries.First().Code.Should().Be("GB");

        cache.Species.Should().HaveCount(1);
        cache.Species.First().Code.Should().Be("CATTLE");

        cache.Roles.Should().HaveCount(1);
        cache.Roles.First().Code.Should().Be("KEEPER");

        cache.SiteIdentifierTypes.Should().HaveCount(1);
        cache.SiteIdentifierTypes.First().Code.Should().Be("CPH");

        cache.ProductionUsages.Should().HaveCount(1);
        cache.ProductionUsages.First().Code.Should().Be("MEAT");

        cache.ActivityMaps.Should().HaveCount(1);
        cache.ActivityMaps.First().FacilityActivityCode.Should().Be("FA01");

        cache.SiteTypes.Should().HaveCount(1);
        cache.SiteTypes.First().Code.Should().Be("FARM");

        cache.SiteActivityTypes.Should().HaveCount(1);
        cache.SiteActivityTypes.First().Code.Should().Be("KEEPING");
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyRepositories_ReturnsEmptyCollections()
    {
        // Arrange
        _countryRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CountryDocument>());
        _speciesRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SpeciesDocument>());
        _roleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<RoleDocument>());
        _siteIdentifierTypeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SiteIdentifierTypeDocument>());
        _productionUsageRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ProductionUsageDocument>());
        _activityMapRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<FacilityBusinessActivityMapDocument>());
        _siteTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteTypeListDocument?)null);
        _siteActivityTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteActivityTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteActivityTypeListDocument?)null);
        _siteTypeMapListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeMapListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteTypeMapListDocument?)null);

        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        await cache.InitializeAsync();

        // Assert
        cache.Countries.Should().NotBeNull().And.BeEmpty();
        cache.Species.Should().NotBeNull().And.BeEmpty();
        cache.Roles.Should().NotBeNull().And.BeEmpty();
        cache.SiteIdentifierTypes.Should().NotBeNull().And.BeEmpty();
        cache.ProductionUsages.Should().NotBeNull().And.BeEmpty();
        cache.ActivityMaps.Should().NotBeNull().And.BeEmpty();
        cache.SiteTypes.Should().NotBeNull().And.BeEmpty();
        cache.SiteActivityTypes.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_ReloadsData()
    {
        // Arrange
        var firstCountries = new List<CountryDocument>
        {
            new() { IdentifierId = "c1", Code = "GB", Name = "United Kingdom", IsActive = true }
        };
        var secondCountries = new List<CountryDocument>
        {
            new() { IdentifierId = "c1", Code = "GB", Name = "United Kingdom", IsActive = true },
            new() { IdentifierId = "c2", Code = "IE", Name = "Ireland", IsActive = true }
        };

        var callCount = 0;
        _countryRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ == 0 ? firstCountries : secondCountries);
        _speciesRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SpeciesDocument>());
        _roleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<RoleDocument>());
        _siteIdentifierTypeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SiteIdentifierTypeDocument>());
        _productionUsageRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ProductionUsageDocument>());
        _activityMapRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<FacilityBusinessActivityMapDocument>());
        _siteTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteTypeListDocument?)null);
        _siteActivityTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteActivityTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteActivityTypeListDocument?)null);
        _siteTypeMapListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeMapListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteTypeMapListDocument?)null);

        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        await cache.InitializeAsync();
        var firstCount = cache.Countries.Count;

        await cache.InitializeAsync();
        var secondCount = cache.Countries.Count;

        // Assert
        firstCount.Should().Be(1);
        secondCount.Should().Be(2);
    }

    [Fact]
    public async Task InitializeAsync_WhenRepositoryThrowsException_LogsErrorAndThrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");
        _countryRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        Func<Task> act = async () => await cache.InitializeAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load reference data into cache")),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_CallsAllRepositories()
    {
        // Arrange
        SetupDefaultRepositoryResponses();

        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        await cache.InitializeAsync();

        // Assert
        _countryRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _speciesRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _roleRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _siteIdentifierTypeRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _productionUsageRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _activityMapRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _siteTypeListRepository.Verify(
            x => x.FindOneAsync(
                It.IsAny<Expression<Func<SiteTypeListDocument, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _siteActivityTypeListRepository.Verify(
            x => x.FindOneAsync(
                It.IsAny<Expression<Func<SiteActivityTypeListDocument, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _siteTypeMapListRepository.Verify(
            x => x.FindOneAsync(
                It.IsAny<Expression<Func<SiteTypeMapListDocument, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_LogsInformationMessages()
    {
        // Arrange
        SetupDefaultRepositoryResponses();

        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        await cache.InitializeAsync();

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading reference data into in-memory cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Reference data cache loaded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_CallsInitializeAsync()
    {
        // Arrange
        SetupDefaultRepositoryResponses();

        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        await ((IHostedService)cache).StartAsync(CancellationToken.None);

        // Assert
        _countryRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        cache.Countries.Should().NotBeNull();
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange
        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        Func<Task> act = async () => await ((IHostedService)cache).StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_WithCancellationToken_PassesToRepositories()
    {
        // Arrange
        SetupDefaultRepositoryResponses();
        var cts = new CancellationTokenSource();
        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        await cache.InitializeAsync(cts.Token);

        // Assert
        _countryRepository.Verify(x => x.GetAllAsync(cts.Token), Times.Once);
        _speciesRepository.Verify(x => x.GetAllAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_ConcurrentCalls_AllComplete()
    {
        // Arrange
        SetupDefaultRepositoryResponses();
        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        var task1 = cache.InitializeAsync();
        var task2 = cache.InitializeAsync();
        var task3 = cache.InitializeAsync();

        await Task.WhenAll(task1, task2, task3);

        // Assert - All three calls should complete successfully
        // The semaphore ensures they execute sequentially, but all three will run
        _countryRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public void Countries_BeforeInitialization_ReturnsEmptyCollection()
    {
        // Arrange
        var cache = new ReferenceDataCache(_serviceScopeFactory.Object, _logger.Object);

        // Act
        var countries = cache.Countries;

        // Assert
        countries.Should().NotBeNull().And.BeEmpty();
    }

    private void SetupDefaultRepositoryResponses()
    {
        _countryRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CountryDocument>());
        _speciesRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SpeciesDocument>());
        _roleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<RoleDocument>());
        _siteIdentifierTypeRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<SiteIdentifierTypeDocument>());
        _productionUsageRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ProductionUsageDocument>());
        _activityMapRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<FacilityBusinessActivityMapDocument>());
        _siteTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteTypeListDocument?)null);
        _siteActivityTypeListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteActivityTypeListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteActivityTypeListDocument?)null);
        _siteTypeMapListRepository.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<SiteTypeMapListDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteTypeMapListDocument?)null);
    }
}