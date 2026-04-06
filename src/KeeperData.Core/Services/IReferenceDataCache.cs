using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IReferenceDataCache
{
    IReadOnlyCollection<CountryDocument> Countries { get; }

    IReadOnlyCollection<SpeciesDocument> Species { get; }

    IReadOnlyCollection<RoleDocument> Roles { get; }

    IReadOnlyCollection<SiteTypeDocument> SiteTypes { get; }

    IReadOnlyCollection<SiteActivityTypeDocument> SiteActivityTypes { get; }

    IReadOnlyCollection<SiteIdentifierTypeDocument> SiteIdentifierTypes { get; }

    IReadOnlyCollection<ProductionUsageDocument> ProductionUsages { get; }

    IReadOnlyCollection<FacilityBusinessActivityMapDocument> ActivityMaps { get; }

    IReadOnlyCollection<SiteTypeMapDocument> SiteTypeMaps { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

}