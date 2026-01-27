using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Extensions;
using MongoDB.Driver;
using System.Linq;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamHoldingMapper
{
    public static async Task<List<SamHoldingDocument>> ToSilver(
        List<SamCphHolding> rawHoldings,
        Func<string?, CancellationToken, Task<(string? PremiseActivityTypeId, string? PremiseActivityTypeName)>> resolvePremiseActivityType,
        Func<string?, CancellationToken, Task<(string? PremiseTypeId, string? PremiseTypeName)>> resolvePremiseType,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamHoldingDocument>();

        foreach (var h in rawHoldings?.Where(x => x.CPH != null) ?? [])
        {
            var holding = await ToSilver(
                h,
                resolvePremiseActivityType,
                resolvePremiseType,
                resolveCountry,
                cancellationToken);

            result.Add(holding);
        }

        return result;
    }

    public static async Task<SamHoldingDocument> ToSilver(
        SamCphHolding h,
        Func<string?, CancellationToken, Task<(string? PremiseActivityTypeId, string? PremiseActivityTypeName)>> resolvePremiseActivityType,
        Func<string?, CancellationToken, Task<(string? PremiseTypeId, string? PremiseTypeName)>> resolvePremiseType,
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

            PremiseActivityTypeId = null,
            PremiseActivityTypeCode = null,

            PremiseTypeIdentifier = null,
            PremiseTypeCode = null,

            SpeciesTypeCode = h.AnimalSpeciesCodeUnwrapped,
            ProductionUsageCodeList = [.. h.AnimalProductionUsageCodeList.Select(ProductionUsageCodeFormatters.TrimProductionUsageCodeHolding)],

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
            },

            GroupMarks = []
        };

        return result;
    }

    public static async Task<SiteDocument?> ToGold(
        string goldSiteId,
        SiteDocument? existingSite,
        List<SamHoldingDocument> silverHoldings,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        List<PartyDocument> goldParties,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<PremisesTypeDocument?>> getPremiseTypeById,
        Func<string?, CancellationToken, Task<SiteIdentifierTypeDocument?>> getSiteIdentifierTypeByCode,
        Func<string?, CancellationToken, Task<(string? speciesTypeId, string? speciesTypeName)>> findSpecies,
        Func<string?, CancellationToken, Task<PremisesActivityTypeDocument?>> getPremiseActivityTypeByCode,
        CancellationToken cancellationToken)
    {
        if (silverHoldings == null || silverHoldings.Count == 0)
            return null;

        var representative = silverHoldings.Any(x => x.HoldingStatus == HoldingStatusType.Active.GetDescription())
            ? silverHoldings.Where(x => x.HoldingStatus == HoldingStatusType.Active.GetDescription()).OrderByDescending(h => h.LastUpdatedDate).First()
            : silverHoldings.OrderByDescending(h => h.LastUpdatedDate).First();

        var distinctSpecies = await GetDistinctReferenceDataAsync<SpeciesDocument>(
            silverHoldings.Select(h => h.SpeciesTypeCode),
            findSpecies,
            cancellationToken);

        var distinctPremiseActivities = await GetDistinctReferenceDataAsync(
            silverHoldings.Select(h => h.PremiseActivityTypeCode),
            getPremiseActivityTypeByCode,
            cancellationToken);

        var species = distinctSpecies
            .Where(doc => doc.typeId is not null)
            .Select(doc => Species.Create(
                id: doc.typeId ?? string.Empty,
                lastUpdatedDate: representative.LastUpdatedDate,
                code: doc.searchValue,
                name: doc.typeName ?? string.Empty))
            .ToList();

        var activities = distinctPremiseActivities
            .Select(doc => SiteActivity.Create(
                id: doc.IdentifierId,
                type: doc.ToDomain(),
                startDate: representative.HoldingStartDate,
                endDate: representative.HoldingEndDate,
                lastUpdatedDate: representative.LastUpdatedDate))
            .ToList();

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
                existingSite,
                goldSiteGroupMarks,
                goldParties,
                getCountryById,
                getPremiseTypeById,
                species,
                activities,
                cphnSiteIdentifierType,
                cancellationToken)
            : await CreateSiteAsync(
                goldSiteId,
                representative,
                goldSiteGroupMarks,
                goldParties,
                getCountryById,
                getPremiseTypeById,
                species,
                activities,
                cphnSiteIdentifierType,
                cancellationToken);

        return SiteDocument.FromDomain(site);
    }

    private static async Task<Site> CreateSiteAsync(
        string goldSiteId,
        SamHoldingDocument representative,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        List<PartyDocument> goldParties,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<PremisesTypeDocument?>> getPremiseTypeById,
        List<Species> species,
        List<SiteActivity> activities,
        SiteIdentifierType? siteIdentifierType,
        CancellationToken cancellationToken)
    {
        var premiseTypeLookup = await GetPremiseTypeAsync(
            representative.PremiseTypeIdentifier,
            getPremiseTypeById,
            cancellationToken);

        var premiseType = premiseTypeLookup == null ? null : PremisesType.Create(
            premiseTypeLookup.IdentifierId,
            premiseTypeLookup.Code,
            premiseTypeLookup.Name,
            premiseTypeLookup.LastModifiedDate);

        var address = await LocationMapper.AddressToGold(representative.Location?.Address, getCountryById, cancellationToken);
        var communication = LocationMapper.CommunicationToGold(representative.Communication);

        var location = Location.Create(
            representative.Location?.OsMapReference,
            representative.Location?.Easting,
            representative.Location?.Northing,
            address,
            communication: [communication]);

        var groupMarks = ToGroupMarks(goldSiteGroupMarks);

        var siteParties = goldParties
            .Where(p => !p.Deleted && !string.IsNullOrWhiteSpace(p.CustomerNumber))
            .Select(p => p.ToSitePartyDomain(representative.LastUpdatedDate))
            .ToList();

        var site = Site.Create(
            goldSiteId,
            representative.CreatedDate,
            representative.LastUpdatedDate,
            representative.LocationName ?? string.Empty,
            representative.HoldingStartDate,
            representative.HoldingEndDate,
            representative.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            representative.Deleted,
            premiseType,
            location);

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
        site.SetSiteParties(goldSiteId, siteParties, representative.LastUpdatedDate);

        return site;
    }

    private static async Task<Site> UpdateSiteAsync(
        SamHoldingDocument representative,
        SiteDocument existing,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        List<PartyDocument> goldParties,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<PremisesTypeDocument?>> getPremiseTypeById,
        List<Species> species,
        List<SiteActivity> activities,
        SiteIdentifierType? siteIdentifierType,
        CancellationToken cancellationToken)
    {
        var site = existing.ToDomain();

        var groupMarks = ToGroupMarks(goldSiteGroupMarks);

        var siteParties = goldParties
            .Where(p => !p.Deleted && !string.IsNullOrWhiteSpace(p.CustomerNumber))
            .Select(p => p.ToSitePartyDomain(representative.LastUpdatedDate))
            .ToList();

        site.Update(
            representative.LastUpdatedDate,
            representative.LocationName ?? string.Empty,
            representative.HoldingStartDate,
            representative.HoldingEndDate,
            representative.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            representative.Deleted);

        var updatedAddress = await LocationMapper.AddressToGold(representative.Location?.Address, getCountryById, cancellationToken);
        var updatedCommunication = LocationMapper.CommunicationToGold(representative.Communication);

        if (representative.PremiseTypeIdentifier != site.Type?.Id)
        {
            var premiseTypeLookup = await GetPremiseTypeAsync(
                representative.PremiseTypeIdentifier,
                getPremiseTypeById,
                cancellationToken);

            var premiseType = premiseTypeLookup == null ? null : PremisesType.Create(
                premiseTypeLookup.IdentifierId,
                premiseTypeLookup.Code,
                premiseTypeLookup.Name,
                premiseTypeLookup.LastModifiedDate);

            site.SetPremisesType(premiseType, representative.LastUpdatedDate);
        }

        site.SetLocation(
            representative.LastUpdatedDate,
            representative.Location?.OsMapReference,
            representative.Location?.Easting,
            representative.Location?.Northing,
            updatedAddress,
            [updatedCommunication]);

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
        site.SetSiteParties(existing.Id, siteParties, representative.LastUpdatedDate);

        return site;
    }

    private static async Task<PremisesTypeDocument?> GetPremiseTypeAsync(
        string? premiseTypeIdentifier,
        Func<string?, CancellationToken, Task<PremisesTypeDocument?>> getPremiseTypeById,
        CancellationToken cancellationToken)
    {
        if (premiseTypeIdentifier == null) return null;

        return await getPremiseTypeById(premiseTypeIdentifier, cancellationToken);
    }

    private static async Task<List<(string searchValue, string? typeId, string? typeName)>> GetDistinctReferenceDataAsync<T>(
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

    private static async Task<List<T>> GetDistinctReferenceDataAsync<T>(
        IEnumerable<string?> rawCodes,
        Func<string?, CancellationToken, Task<T?>> getTypeByCodeAsync,
        CancellationToken cancellationToken)
    {
        var distinctCodes = rawCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToList();

        var tasks = distinctCodes
            .Select(async code =>
            {
                var type = await getTypeByCodeAsync(code, cancellationToken);
                return type;
            });

        var results = await Task.WhenAll(tasks);
        return [.. results.OfType<T>()];
    }

    private static List<GroupMark> ToGroupMarks(List<SiteGroupMarkRelationshipDocument> relationships)
    {
        return [.. relationships
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
            })];
    }
}