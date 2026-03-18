using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IReferenceDataCache
{
    IReadOnlyCollection<CountryDocument> Countries { get; }

    IReadOnlyCollection<SpeciesDocument> Species { get; }

    IReadOnlyCollection<RoleDocument> Roles { get; }

    IReadOnlyCollection<PremisesTypeDocument> PremisesTypes { get; }

    IReadOnlyCollection<PremisesActivityTypeDocument> PremisesActivityTypes { get; }

    IReadOnlyCollection<SiteIdentifierTypeDocument> SiteIdentifierTypes { get; }

    IReadOnlyCollection<ProductionUsageDocument> ProductionUsages { get; }

    IReadOnlyCollection<FacilityBusinessActivityMapDocument> ActivityMaps { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

}