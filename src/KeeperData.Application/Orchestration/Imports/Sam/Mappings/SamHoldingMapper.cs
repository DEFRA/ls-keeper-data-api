using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using Microsoft.Extensions.Logging;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Extensions;
using KeeperData.Core.Services;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamHoldingMapper
{
    public static async Task<List<SamHoldingDocument>> ToSilver(
        List<SamCphHolding> rawHoldings,
        Func<string?, CancellationToken, Task<(string? SiteActivityTypeId, string? SiteActivityTypeName)>> resolveSiteActivityType,
        Func<string?, CancellationToken, Task<(string? SiteTypeId, string? SiteTypeName)>> resolveSiteType,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamHoldingDocument>();

        foreach (var h in rawHoldings?.Where(x => x.CPH != null) ?? [])
        {
            var holding = await ToSilver(
                h,
                resolveSiteActivityType,
                resolveSiteType,
                resolveCountry,
                cancellationToken);

            result.Add(holding);
        }

        return result;
    }

    public static async Task<SamHoldingDocument> ToSilver(
        SamCphHolding h,
        Func<string?, CancellationToken, Task<(string? SiteActivityTypeId, string? SiteActivityTypeName)>> resolveSiteActivityType,
        Func<string?, CancellationToken, Task<(string? SiteTypeId, string? SiteTypeName)>> resolveSiteType,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var addressLine = AddressFormatters.FormatAddressRange(
                            h.SAON_START_NUMBER, h.SAON_START_NUMBER_SUFFIX,
                            h.SAON_END_NUMBER, h.SAON_END_NUMBER_SUFFIX,
                            h.PAON_START_NUMBER, h.PAON_START_NUMBER_SUFFIX,
                            h.PAON_END_NUMBER, h.PAON_END_NUMBER_SUFFIX,
                            h.SAON_DESCRIPTION, h.PAON_DESCRIPTION);

        var (countryId, countryCode, _) = await resolveCountry(h.COUNTRY_CODE, h.UK_INTERNAL_CODE, cancellationToken);

        var result = new SamHoldingDocument
        {
            // Id - Leave to support upsert assigning Id

            LastUpdatedBatchId = h.BATCH_ID,
            CreatedDate = h.CreatedAtUtc ?? DateTime.UtcNow,
            LastUpdatedDate = h.UpdatedAtUtc ?? DateTime.UtcNow,
            Deleted = h.IsDeleted ?? false,

            CountyParishHoldingNumber = h.CPH,
            AlternativeHoldingIdentifier = null,

            CphRelationshipType = h.CPH_RELATIONSHIP_TYPE,
            SecondaryCph = h.SecondaryCphUnwrapped,

            CphTypeIdentifier = h.CPH_TYPE,
            LocationName = h.FEATURE_NAME,

            DiseaseType = h.DISEASE_TYPE,
            Interval = h.INTERVAL,
            IntervalUnitOfTime = h.INTERVAL_UNIT_OF_TIME,

            HoldingStartDate = h.FEATURE_ADDRESS_FROM_DATE,
            HoldingEndDate = h.FEATURE_ADDRESS_TO_DATE,
            HoldingStatus = HoldingStatusFormatters.FormatHoldingStatus(h.IsDeleted ?? false),

            MovementRestrictionReasonCode = h.MOVEMENT_RSTRCTN_RSN_CODE,

            SourceFacilityTypeCode = h.FACILITY_TYPE_CODE,
            SourceFacilityBusinessActivityCode = h.FACILITY_BUSINSS_ACTVTY_CODE,
            SourceFacilitySubBusinessActivityCode = h.FCLTY_SUB_BSNSS_ACTVTY_CODE,

            SiteActivityTypeId = null,
            SiteActivityTypeCode = null,

            SiteTypeIdentifier = null,
            SiteTypeCode = null,

            SpeciesTypeCode = h.AnimalSpeciesCodeUnwrapped,
            ProductionUsageCodeList = [.. h.AnimalProductionUsageCodeList.Select(ProductionUsageCodeFormatters.TrimProductionUsageCodeHolding).Distinct()],

            Location = new Core.Documents.Silver.LocationDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Easting = h.EASTING,
                Northing = h.NORTHING,
                OsMapReference = h.OS_MAP_REFERENCE,
                Address = new Core.Documents.Silver.AddressDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    AddressLine = addressLine,
                    AddressLocality = h.LOCALITY,
                    AddressStreet = h.STREET,
                    AddressTown = h.TOWN,
                    AddressPostCode = h.POSTCODE,
                    CountrySubDivision = h.UK_INTERNAL_CODE,

                    CountryIdentifier = countryId,
                    CountryCode = countryCode,

                    UniquePropertyReferenceNumber = h.UDPRN
                }
            },

            Communication = new Core.Documents.Silver.CommunicationDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Email = null,
                Mobile = null,
                Landline = null
            }
        };

        return result;
    }

    internal static SamHoldingDocument SelectRepresentativeHolding(List<SamHoldingDocument> silverHoldings, ILogger? logger = null)
    {
        const string commonLandBusinessUsage = "Common Land";
        var activeStatus = HoldingStatusType.Active.GetDescription();

        // Priority 1: Active SAM Holding (not Common Land)
        var activeSamHolding = silverHoldings
            .Where(x => x.HoldingStatus == activeStatus && x.SourceFacilitySubBusinessActivityCode != commonLandBusinessUsage)
            .OrderByDescending(h => h.LastUpdatedDate)
            .FirstOrDefault();

        if (activeSamHolding != null)
        {
            logger?.LogInformation(
                "SelectRepresentativeHolding: Priority 1 (active, not Common Land) selected for CPH {Cph}: AddressLine={AddressLine}, Street={Street}, Town={Town}, PostCode={PostCode}",
                activeSamHolding.CountyParishHoldingNumber,
                activeSamHolding.Location?.Address?.AddressLine,
                activeSamHolding.Location?.Address?.AddressStreet,
                activeSamHolding.Location?.Address?.AddressTown,
                activeSamHolding.Location?.Address?.AddressPostCode);
            return activeSamHolding;
        }

        // Priority 2: Any SAM Holding (not Common Land)
        var samHolding = silverHoldings
            .Where(x => x.SourceFacilitySubBusinessActivityCode != commonLandBusinessUsage)
            .OrderByDescending(h => h.LastUpdatedDate)
            .FirstOrDefault();

        if (samHolding != null)
        {
            logger?.LogInformation(
                "SelectRepresentativeHolding: Priority 2 (any, not Common Land) selected for CPH {Cph}: AddressLine={AddressLine}, Street={Street}, Town={Town}, PostCode={PostCode}",
                samHolding.CountyParishHoldingNumber,
                samHolding.Location?.Address?.AddressLine,
                samHolding.Location?.Address?.AddressStreet,
                samHolding.Location?.Address?.AddressTown,
                samHolding.Location?.Address?.AddressPostCode);
            return samHolding;
        }

        // Priority 3: Active Common Land
        var activeCommonLand = silverHoldings
            .Where(x => x.HoldingStatus == activeStatus)
            .OrderByDescending(h => h.LastUpdatedDate)
            .FirstOrDefault();

        if (activeCommonLand != null)
        {
            logger?.LogInformation(
                "SelectRepresentativeHolding: Priority 3 (active Common Land) selected for CPH {Cph}: AddressLine={AddressLine}, Street={Street}, Town={Town}, PostCode={PostCode}",
                activeCommonLand.CountyParishHoldingNumber,
                activeCommonLand.Location?.Address?.AddressLine,
                activeCommonLand.Location?.Address?.AddressStreet,
                activeCommonLand.Location?.Address?.AddressTown,
                activeCommonLand.Location?.Address?.AddressPostCode);
            return activeCommonLand;
        }

        // Priority 4: Any holding (fallback)
        var fallback = silverHoldings.OrderByDescending(h => h.LastUpdatedDate).First();
        logger?.LogInformation(
            "SelectRepresentativeHolding: Priority 4 (fallback) selected for CPH {Cph}: AddressLine={AddressLine}, Street={Street}, Town={Town}, PostCode={PostCode}",
            fallback.CountyParishHoldingNumber,
            fallback.Location?.Address?.AddressLine,
            fallback.Location?.Address?.AddressStreet,
            fallback.Location?.Address?.AddressTown,
            fallback.Location?.Address?.AddressPostCode);
        return fallback;
    }

    public static SamHoldingDocument SelectAddressSource(List<SamHoldingDocument> silverHoldings, ILogger? logger = null)
    {
        // Prefer the document that came directly from the common lands API endpoint — it is the
        // authoritative address source. A holding document can also carry
        // SourceFacilitySubBusinessActivityCode == "Common Land" but its address originates from
        // the SAM holdings table, which must not override the common land address.
        var commonLand = silverHoldings
            .FirstOrDefault(x => x.IsFromCommonLandSource);

        if (commonLand != null)
        {
            logger?.LogInformation(
                "SelectAddressSource: using Common Land source address for CPH {Cph}: AddressLine={AddressLine}, Street={Street}, Town={Town}, PostCode={PostCode}",
                commonLand.CountyParishHoldingNumber,
                commonLand.Location?.Address?.AddressLine,
                commonLand.Location?.Address?.AddressStreet,
                commonLand.Location?.Address?.AddressTown,
                commonLand.Location?.Address?.AddressPostCode);
            return commonLand;
        }

        logger?.LogInformation(
            "SelectAddressSource: no Common Land source found for CPH {Cph}, falling back to representative holding",
            silverHoldings.FirstOrDefault()?.CountyParishHoldingNumber);
        return SelectRepresentativeHolding(silverHoldings, logger);
    }

    private static string ResolveSiteName(SamHoldingDocument representative, SamHoldingDocument addressSource)
        => addressSource.IsFromCommonLandSource
            ? addressSource.LocationName ?? string.Empty
            : representative.LocationName ?? string.Empty;

    public static async Task<SiteDocument?> ToGold(
        string goldSiteId,
        SiteDocument? existingSite,
        List<SamHoldingDocument> silverHoldings,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        List<PartyDocument> goldParties,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<SiteTypeDocument?>> getSiteTypeByCode,
        Func<string?, CancellationToken, Task<SiteIdentifierTypeDocument?>> getSiteIdentifierTypeByCode,
        Func<string?, CancellationToken, Task<(string? speciesTypeId, string? speciesTypeName)>> findSpecies,
        Func<string?, CancellationToken, Task<SiteActivityTypeDocument?>> getSiteActivityTypeByCode,
        ISiteTypeDerivedCodeLookupService derivedCodeLookupService,
        CancellationToken cancellationToken,
        ILogger? logger = null)
    {
        if (silverHoldings == null || silverHoldings.Count == 0)
            return null;

        // Prefer SAM Holding over Common Land when selecting representative
        var representative = SelectRepresentativeHolding(silverHoldings, logger);

        // Common land address takes precedence over site address for location data
        var addressSource = SelectAddressSource(silverHoldings, logger);

        var distinctSpecies = await GetDistinctReferenceDataAsync(
            silverHoldings.Select(h => h.SpeciesTypeCode),
            findSpecies,
            cancellationToken);

        var species = distinctSpecies
            .Where(doc => doc.typeId is not null)
            .Select(doc => Species.Create(
                id: doc.typeId ?? string.Empty,
                lastUpdatedDate: representative.LastUpdatedDate,
                code: doc.searchValue,
                name: doc.typeName ?? string.Empty))
            .ToList();

        var (allDerivedActivities, derivedSiteType) = await ResolveSiteTypeAndActivitiesAsync(
            silverHoldings,
            derivedCodeLookupService,
            getSiteTypeByCode,
            getSiteActivityTypeByCode,
            representative,
            cancellationToken);

        var cphnSiteIdentifierTypeDocument = await getSiteIdentifierTypeByCode(
            HoldingIdentifierType.CPHN.ToString(),
            cancellationToken);

        var cphnSiteIdentifierType = cphnSiteIdentifierTypeDocument == null ? null : new SiteIdentifierType(
            cphnSiteIdentifierTypeDocument.IdentifierId,
            cphnSiteIdentifierTypeDocument.Code,
            cphnSiteIdentifierTypeDocument.Name,
            cphnSiteIdentifierTypeDocument.LastModifiedDate);

        var site = existingSite is not null
            ? await UpdateSiteAsync(
                representative,
                addressSource,
                existingSite,
                goldSiteGroupMarks,
                goldParties,
                getCountryById,
                species,
                allDerivedActivities,
                derivedSiteType,
                cphnSiteIdentifierType,
                cancellationToken)
            : await CreateSiteAsync(
                goldSiteId,
                representative,
                addressSource,
                goldSiteGroupMarks,
                goldParties,
                getCountryById,
                species,
                allDerivedActivities,
                derivedSiteType,
                cphnSiteIdentifierType,
                cancellationToken);

        return SiteDocument.FromDomain(site);
    }

    private static async Task<(List<SiteActivity>, SiteType?)> ResolveSiteTypeAndActivitiesAsync(
        List<SamHoldingDocument> silverHoldings,
        ISiteTypeDerivedCodeLookupService derivedCodeLookupService,
        Func<string?, CancellationToken, Task<SiteTypeDocument?>> getSiteTypeByCode,
        Func<string?, CancellationToken, Task<SiteActivityTypeDocument?>> getSiteActivityTypeByCode,
        SamHoldingDocument representative,
        CancellationToken cancellationToken)
    {
        var allDerivedActivities = new List<SiteActivity>();
        SiteType? derivedSiteType = null;

        foreach (var holding in silverHoldings)
        {
            var derivedResult = derivedCodeLookupService.Resolve(holding.SourceFacilitySubBusinessActivityCode);
            if (derivedResult == null)
                continue;

            derivedSiteType ??= await ResolveSiteTypeAsync(derivedResult.SiteTypeCode, getSiteTypeByCode, cancellationToken);

            await ResolveAndAddActivitiesAsync(
                derivedResult.Activities,
                allDerivedActivities,
                getSiteActivityTypeByCode,
                representative,
                cancellationToken);
        }

        // If no derived mapping resolved a site type, fall back to the explicit site type code on the representative
        derivedSiteType ??= await ResolveSiteTypeAsync(representative.SiteTypeCode, getSiteTypeByCode, cancellationToken);

        return (allDerivedActivities, derivedSiteType);
    }

    private static async Task<SiteType?> ResolveSiteTypeAsync(
        string? siteTypeCode,
        Func<string?, CancellationToken, Task<SiteTypeDocument?>> getSiteTypeByCode,
        CancellationToken cancellationToken)
    {
        var siteTypeLookup = await getSiteTypeByCode(siteTypeCode, cancellationToken);

        return siteTypeLookup == null
            ? null
            : SiteType.Create(
                siteTypeLookup.IdentifierId,
                siteTypeLookup.Code,
                siteTypeLookup.Name,
                siteTypeLookup.LastModifiedDate);
    }

    private static async Task ResolveAndAddActivitiesAsync(
        List<SiteTypeDerivedActivityResult> derivedActivities,
        List<SiteActivity> allDerivedActivities,
        Func<string?, CancellationToken, Task<SiteActivityTypeDocument?>> getSiteActivityTypeByCode,
        SamHoldingDocument representative,
        CancellationToken cancellationToken)
    {
        foreach (var derivedActivity in derivedActivities)
        {
            if (IsActivityAlreadyAdded(allDerivedActivities, derivedActivity.Code))
                continue;

            var siteActivity = await CreateSiteActivityAsync(
                derivedActivity.Code,
                getSiteActivityTypeByCode,
                representative,
                cancellationToken);

            if (siteActivity != null)
            {
                allDerivedActivities.Add(siteActivity);
            }
        }
    }

    private static bool IsActivityAlreadyAdded(List<SiteActivity> activities, string? activityCode)
    {
        return activities.Any(a => a.Type.Code.Equals(activityCode, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<SiteActivity?> CreateSiteActivityAsync(
        string? activityCode,
        Func<string?, CancellationToken, Task<SiteActivityTypeDocument?>> getSiteActivityTypeByCode,
        SamHoldingDocument representative,
        CancellationToken cancellationToken)
    {
        var activityDoc = await getSiteActivityTypeByCode(activityCode, cancellationToken);

        return activityDoc == null
            ? null
            : SiteActivity.Create(
                id: activityDoc.IdentifierId,
                type: activityDoc.ToDomain(),
                startDate: representative.HoldingStartDate,
                endDate: representative.HoldingEndDate,
                lastUpdatedDate: representative.LastUpdatedDate);
    }

    private static async Task<Site> CreateSiteAsync(
        string goldSiteId,
        SamHoldingDocument representative,
        SamHoldingDocument addressSource,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        List<PartyDocument> goldParties,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        List<Species> species,
        List<SiteActivity> activities,
        SiteType? siteType,
        SiteIdentifierType? siteIdentifierType,
        CancellationToken cancellationToken)
    {
        var (address, communication) = await ResolveLocationPartsAsync(addressSource, getCountryById, cancellationToken);
        var isPermanentLandHolding = representative.CphRelationshipType.IsPermanentLandHolding();

        var location = Location.Create(
            addressSource.Location?.OsMapReference,
            addressSource.Location?.Easting,
            addressSource.Location?.Northing,
            address,
            communication: [communication]);

        var site = Site.Create(
            goldSiteId,
            representative.CreatedDate,
            representative.LastUpdatedDate,
            ResolveSiteName(representative, addressSource),
            representative.HoldingStartDate,
            representative.HoldingEndDate,
            representative.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            representative.Deleted,
            isPermanentLandHolding ? null : representative.SecondaryCph,
            string.IsNullOrEmpty(representative.CphTypeIdentifier) ? null : representative.CphTypeIdentifier,
            siteType,
            location,
            isPermanentLandHolding ? representative.SecondaryCph : null);

        ApplySiteData(site, goldSiteId, representative, goldSiteGroupMarks, goldParties, species, activities, siteIdentifierType);

        return site;
    }

    private static async Task<Site> UpdateSiteAsync(
        SamHoldingDocument representative,
        SamHoldingDocument addressSource,
        SiteDocument existing,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        List<PartyDocument> goldParties,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        List<Species> species,
        List<SiteActivity> activities,
        SiteType? siteType,
        SiteIdentifierType? siteIdentifierType,
        CancellationToken cancellationToken)
    {
        var isPermanentLandHolding = representative.CphRelationshipType.IsPermanentLandHolding();
        var site = existing.ToDomain();

        site.Update(
            representative.LastUpdatedDate,
            ResolveSiteName(representative, addressSource),
            representative.HoldingStartDate,
            representative.HoldingEndDate,
            representative.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            representative.Deleted,
            isPermanentLandHolding ? null : representative.SecondaryCph,
            string.IsNullOrEmpty(representative.CphTypeIdentifier) ? null : representative.CphTypeIdentifier,
            isPermanentLandHolding ? representative.SecondaryCph : null);

        var (updatedAddress, updatedCommunication) = await ResolveLocationPartsAsync(addressSource, getCountryById, cancellationToken);

        // Always set the derived site type (may be null if no mapping found).
        site.SetSiteType(siteType, representative.LastUpdatedDate);

        site.SetLocation(
            representative.LastUpdatedDate,
            addressSource.Location?.OsMapReference,
            addressSource.Location?.Easting,
            addressSource.Location?.Northing,
            updatedAddress,
            [updatedCommunication]);

        ApplySiteData(site, existing.Id, representative, goldSiteGroupMarks, goldParties, species, activities, siteIdentifierType);

        return site;
    }


    private static async Task<(Address address, Communication communication)> ResolveLocationPartsAsync(
        SamHoldingDocument representative,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        CancellationToken cancellationToken)
    {
        var address = await LocationMapper.AddressToGold(representative.Location?.Address, getCountryById, cancellationToken);
        var communication = LocationMapper.CommunicationToGold(representative.Communication);
        return (address, communication);
    }

    private static async Task<List<(string searchValue, string? typeId, string? typeName)>> GetDistinctReferenceDataAsync(
        IEnumerable<string?> rawCodes,
        Func<string?, CancellationToken, Task<(string? typeId, string? typeName)>> findAsync,
        CancellationToken cancellationToken)
    {
        var distinctCodes = rawCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToList();

        var tasks = distinctCodes
            .Select(async code =>
            {
                var (typeId, typeName) = await findAsync(code, cancellationToken);
                return (searchValue: code!, typeId, typeName);
            });

        var results = await Task.WhenAll(tasks);
        return [.. results];
    }

    private static void ApplySiteData(
        Site site,
        string siteId,
        SamHoldingDocument representative,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        List<PartyDocument> goldParties,
        List<Species> species,
        List<SiteActivity> activities,
        SiteIdentifierType? siteIdentifierType)
    {
        var groupMarks = ToGroupMarks(goldSiteGroupMarks);
        var siteParties = goldParties
            .Where(p => !p.Deleted && !string.IsNullOrWhiteSpace(p.CustomerNumber))
            .Select(p => p.ToSitePartyDomain(representative.LastUpdatedDate))
            .ToList();

        if (siteIdentifierType != null)
        {
            site.SetSiteIdentifier(
                identifierLastUpdatedDate: representative.LastUpdatedDate,
                identifier: representative.CountyParishHoldingNumber,
                type: siteIdentifierType,
                id: null,
                siteLastUpdatedDate: representative.LastUpdatedDate);
        }

        site.SetSpecies(species, representative.LastUpdatedDate);
        site.SetActivities(activities, representative.LastUpdatedDate);
        site.SetGroupMarks(groupMarks, representative.LastUpdatedDate);
        site.SetSiteParties(siteId, siteParties, representative.LastUpdatedDate);
    }

    private static List<GroupMark> ToGroupMarks(List<SiteGroupMarkRelationshipDocument> relationships)
    {
        return
        [
            .. relationships
            .Where(m => !string.IsNullOrWhiteSpace(m.Herdmark))
            .GroupBy(m => m.Herdmark)
            .Select(group =>
            {
                var herdmarkGroup = group.First();

                var speciesList = group
                    .Where(m => m.SpeciesTypeId is not null)
                    .Select(m => Species.Create(
                        id: m.SpeciesTypeId!,
                        lastUpdatedDate: m.LastUpdatedDate,
                        code: m.SpeciesTypeCode ?? string.Empty,
                        name: m.SpeciesTypeName ?? string.Empty))
                    .DistinctBy(s => s.Code)
                    .ToList();

                return new GroupMark(
                    id: herdmarkGroup.Id ?? Guid.NewGuid().ToString(),
                    lastUpdatedDate: herdmarkGroup.LastUpdatedDate,
                    mark: group.Key,
                    startDate: herdmarkGroup.GroupMarkStartDate,
                    endDate: herdmarkGroup.GroupMarkEndDate,
                    species: speciesList);
            })
        ];
    }
}