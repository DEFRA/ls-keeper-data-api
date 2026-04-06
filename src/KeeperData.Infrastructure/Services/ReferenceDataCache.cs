using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// In-memory cache for reference data loaded at application startup.
/// Implements a thread-safe "write rarely, read frequently" pattern where data is loaded once
/// during initialization and read continuously throughout the application lifecycle.
/// </summary>
/// <remarks>
/// Thread Safety:
/// - Write operations are protected by a SemaphoreSlim to ensure only one initialization at a time.
/// - Read operations require no locking - fields are marked as volatile to ensure visibility
///   across threads. The volatile keyword guarantees that reads always get the latest value from
///   memory and writes are immediately visible to all threads, preventing compiler/CPU caching optimizations.
/// - This approach is safe because reference assignments are atomic and collections are immutable once assigned.
///
/// Lifecycle:
/// - Registered as both a singleton IReferenceDataCache and an IHostedService.
/// - StartAsync is called on application startup to populate the cache.
/// - Data can be manually reloaded by calling InitializeAsync.
/// </remarks>
public class ReferenceDataCache(IServiceScopeFactory scopeFactory, ILogger<ReferenceDataCache> logger)
    : IReferenceDataCache, IHostedService
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private volatile IReadOnlyCollection<CountryDocument> _countries = [];
    private volatile IReadOnlyCollection<SpeciesDocument> _species = [];
    private volatile IReadOnlyCollection<RoleDocument> _roles = [];
    private volatile IReadOnlyCollection<SiteTypeDocument> _siteTypes = [];
    private volatile IReadOnlyCollection<SiteActivityTypeDocument> _siteActivityTypes = [];
    private volatile IReadOnlyCollection<SiteIdentifierTypeDocument> _siteIdentifierTypes = [];
    private volatile IReadOnlyCollection<ProductionUsageDocument> _productionUsages = [];
    private volatile IReadOnlyCollection<FacilityBusinessActivityMapDocument> _activityMaps = [];
    private volatile IReadOnlyCollection<SiteTypeMapDocument> _siteTypeMaps = [];

    public IReadOnlyCollection<CountryDocument> Countries => _countries;
    public IReadOnlyCollection<SpeciesDocument> Species => _species;
    public IReadOnlyCollection<RoleDocument> Roles => _roles;
    public IReadOnlyCollection<SiteTypeDocument> SiteTypes => _siteTypes;
    public IReadOnlyCollection<SiteActivityTypeDocument> SiteActivityTypes => _siteActivityTypes;
    public IReadOnlyCollection<SiteIdentifierTypeDocument> SiteIdentifierTypes => _siteIdentifierTypes;
    public IReadOnlyCollection<ProductionUsageDocument> ProductionUsages => _productionUsages;
    public IReadOnlyCollection<FacilityBusinessActivityMapDocument> ActivityMaps => _activityMaps;
    public IReadOnlyCollection<SiteTypeMapDocument> SiteTypeMaps => _siteTypeMaps;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            logger.LogInformation("Loading reference data into in-memory cache...");

            using var scope = scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var countryRepo = provider.GetRequiredService<ICountryRepository>();
            var speciesRepo = provider.GetRequiredService<ISpeciesRepository>();
            var roleRepo = provider.GetRequiredService<IRoleRepository>();
            var siteIdentifierTypeRepo = provider.GetRequiredService<ISiteIdentifierTypeRepository>();
            var productionUsageRepo = provider.GetRequiredService<IProductionUsageRepository>();
            var activityMapRepo = provider.GetRequiredService<IFacilityBusinessActivityMapRepository>();
            var siteTypeListRepo = provider.GetRequiredService<IGenericRepository<SiteTypeListDocument>>();
            var siteActivityTypeListRepo = provider.GetRequiredService<IGenericRepository<SiteActivityTypeListDocument>>();
            var siteTypeMapListRepo = provider.GetRequiredService<IGenericRepository<SiteTypeMapListDocument>>();

            // Start all tasks in parallel
            var countriesTask = countryRepo.GetAllAsync(cancellationToken);
            var speciesTask = speciesRepo.GetAllAsync(cancellationToken);
            var rolesTask = roleRepo.GetAllAsync(cancellationToken);
            var productionUsagesTask = productionUsageRepo.GetAllAsync(cancellationToken);
            var siteIdentifierTypesTask = siteIdentifierTypeRepo.GetAllAsync(cancellationToken);
            var activityMapsTask = activityMapRepo.GetAllAsync(cancellationToken);
            var siteTypeListTask = siteTypeListRepo.FindOneAsync(x => x.Id == SiteTypeListDocument.DocumentId, cancellationToken);
            var siteActivityTypeListTask = siteActivityTypeListRepo.FindOneAsync(x => x.Id == SiteActivityTypeListDocument.DocumentId, cancellationToken);
            var siteTypeMapListTask = siteTypeMapListRepo.FindOneAsync(x => x.Id == SiteTypeMapListDocument.DocumentId, cancellationToken);

            await Task.WhenAll(
                countriesTask,
                speciesTask,
                rolesTask,
                productionUsagesTask,
                siteIdentifierTypesTask,
                activityMapsTask,
                siteTypeListTask,
                siteActivityTypeListTask,
                siteTypeMapListTask
            );

            var countries = await countriesTask;
            var species = await speciesTask;
            var roles = await rolesTask;
            var productionUsages = await productionUsagesTask;
            var siteIdentifierTypes = await siteIdentifierTypesTask;
            var activityMaps = await activityMapsTask;
            var siteTypeList = await siteTypeListTask;
            var siteActivityTypeList = await siteActivityTypeListTask;
            var siteTypeMapList = await siteTypeMapListTask;

            _countries = countries;
            _species = species;
            _roles = roles;
            _productionUsages = productionUsages;
            _siteIdentifierTypes = siteIdentifierTypes;
            _activityMaps = activityMaps;
            _siteTypes = siteTypeList?.Items ?? [];
            _siteActivityTypes = siteActivityTypeList?.Items ?? [];
            _siteTypeMaps = siteTypeMapList?.Items ?? [];

            logger.LogInformation(
                "Reference data cache loaded: {Countries} countries, {Species} species, {Roles} roles, {SiteTypes} site types, {SiteActivityTypes} activity types, {SiteIdentifierTypes} site identifier types, {ProductionUsages} production usages, {ActivityMaps} activity maps, {SiteTypeMaps} site type maps",
                _countries.Count, _species.Count, _roles.Count, _siteTypes.Count,
                _siteActivityTypes.Count, _siteIdentifierTypes.Count,
                _productionUsages.Count, _activityMaps.Count, _siteTypeMaps.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load reference data into cache");
            throw;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
