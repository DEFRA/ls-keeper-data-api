using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Domain.Sites.Formatters;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamHoldingMapper
{
    public static async Task<List<SamHoldingDocument>> ToSilver(
        List<SamCphHolding> rawHoldings,
        Func<string?, CancellationToken, Task<(string? PremiseActivityTypeId, string? PremiseActivityTypeName)>> resolvePremiseActivityType,
        Func<string?, CancellationToken, Task<(string? PremiseTypeId, string? PremiseTypeName)>> resolvePremiseType,
        Func<string?, string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
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
        Func<string?, string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var addressLine = AddressFormatters.FormatAddressRange(
                            h.SAON_START_NUMBER, h.SAON_START_NUMBER_SUFFIX,
                            h.SAON_END_NUMBER, h.SAON_END_NUMBER_SUFFIX,
                            h.PAON_START_NUMBER, h.PAON_START_NUMBER_SUFFIX,
                            h.PAON_END_NUMBER, h.PAON_END_NUMBER_SUFFIX,
                            h.SAON_DESCRIPTION, h.PAON_DESCRIPTION);

        var formattedFacilityBusinessActivityCode = PremiseActivityTypeFormatters.TrimFacilityActivityCode(h.FACILITY_BUSINSS_ACTVTY_CODE);

        var (premiseActivityTypeId, premiseActivityTypeName) = await resolvePremiseActivityType(formattedFacilityBusinessActivityCode, cancellationToken);
        var (premiseTypeId, premiseTypeName) = await resolvePremiseType(h.FACILITY_TYPE_CODE, cancellationToken);
        var (countryId, countryName) = await resolveCountry(h.COUNTRY_CODE, h.UK_INTERNAL_CODE, cancellationToken);

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

            PremiseActivityTypeId = premiseActivityTypeId,
            PremiseActivityTypeCode = formattedFacilityBusinessActivityCode,
            PremiseSubActivityTypeCode = h.FCLTY_SUB_BSNSS_ACTVTY_CODE,

            MovementRestrictionReasonCode = h.MOVEMENT_RSTRCTN_RSN_CODE,

            PremiseTypeIdentifier = premiseTypeId,
            PremiseTypeCode = h.FACILITY_TYPE_CODE,

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
                    CountryCode = h.COUNTRY_CODE,

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
        Func<string?, CancellationToken, Task<(string? speciesTypeId, string? speciesTypeName)>> findSpecies,
        Func<string?, CancellationToken, Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)>> findPremiseActivityType,
        CancellationToken cancellationToken)
    {
        if (silverHoldings == null || silverHoldings.Count == 0)
            return null;

        var representative = silverHoldings.Any(x => x.IsActive)
            ? silverHoldings.Where(x => x.IsActive).OrderByDescending(h => h.LastUpdatedDate).First()
            : silverHoldings.OrderByDescending(h => h.LastUpdatedDate).First();

        var distinctSpecies = await GetDistinctReferenceDataAsync<SpeciesDocument>(
            silverHoldings.Select(h => h.SpeciesTypeCode),
            findSpecies,
            cancellationToken);

        //var distinctProductionUsages = await GetDistinctReferenceDataAsync<ProductionUsageDocument>(
        //    silverHoldings.SelectMany(h => h.ProductionUsageCodeList),
        //    findProductionUsage,
        //    cancellationToken);

        var distinctPremiseActivities = await GetDistinctReferenceDataAsync<PremisesActivityTypeDocument>(
            silverHoldings.Select(h => h.PremiseActivityTypeCode),
            findPremiseActivityType,
            cancellationToken);

        var species = distinctSpecies
            .Where(doc => doc.typeId is not null)
            .Select(doc => new Species(
                id: doc.typeId ?? string.Empty,
                lastUpdatedDate: representative.LastUpdatedDate,
                code: doc.searchValue,
                name: doc.typeName ?? string.Empty))
            .ToList();

        var activities = distinctPremiseActivities
            .Where(doc => doc.typeId is not null)
            .Select(doc => new SiteActivity(
                id: doc.typeId ?? string.Empty,
                activity: doc.searchValue,
                description: doc.typeName,
                startDate: representative.HoldingStartDate,
                endDate: representative.HoldingEndDate,
                lastUpdatedDate: representative.LastUpdatedDate))
            .ToList();

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
        CancellationToken cancellationToken)
    {
        int? uprn = int.TryParse(representative.Location?.Address?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var premiseType = await GetPremiseTypeAsync(
            representative.PremiseTypeIdentifier,
            getPremiseTypeById,
            cancellationToken);

        var country = await GetCountryAsync(
            representative.Location?.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var address = Address.Create(
            uprn,
            representative.Location?.Address?.AddressLine ?? string.Empty,
            representative.Location?.Address?.AddressStreet,
            representative.Location?.Address?.AddressTown,
            representative.Location?.Address?.AddressLocality,
            representative.Location?.Address?.AddressPostCode ?? string.Empty,
            country);

        var communication = Communication.Create(
            representative.Communication?.Email,
            representative.Communication?.Mobile,
            representative.Communication?.Landline,
            false);

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
            premiseType?.Code ?? string.Empty,
            representative.LocationName ?? string.Empty,
            representative.HoldingStartDate,
            representative.HoldingEndDate,
            representative.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            representative.Deleted,
            location);

        site.SetSiteIdentifier(
            lastUpdatedDate: representative.LastUpdatedDate,
            identifier: representative.CountyParishHoldingNumber,
            type: HoldingIdentifierType.CphNumber.ToString());

        site.SetSpecies(species, representative.LastUpdatedDate);

        site.SetActivities(activities, representative.LastUpdatedDate);

        site.SetGroupMarks(groupMarks, representative.LastUpdatedDate);

        site.SetSiteParties(siteParties, representative.LastUpdatedDate);

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
        CancellationToken cancellationToken)
    {
        var site = existing.ToDomain();

        int? uprn = int.TryParse(representative.Location?.Address?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var premiseType = await GetPremiseTypeAsync(
            representative.PremiseTypeIdentifier,
            getPremiseTypeById,
            cancellationToken);

        var country = await GetCountryAsync(
            representative.Location?.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var groupMarks = ToGroupMarks(goldSiteGroupMarks);

        var siteParties = goldParties
            .Where(p => !p.Deleted && !string.IsNullOrWhiteSpace(p.CustomerNumber))
            .Select(p => p.ToSitePartyDomain(representative.LastUpdatedDate))
            .ToList();

        site.Update(
            representative.LastUpdatedDate,
            premiseType?.Code ?? string.Empty,
            representative.LocationName ?? string.Empty,
            representative.HoldingStartDate,
            representative.HoldingEndDate,
            representative.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            representative.Deleted);

        var updatedAddress = Address.Create(
            uprn,
            representative.Location?.Address?.AddressLine ?? string.Empty,
            representative.Location?.Address?.AddressStreet,
            representative.Location?.Address?.AddressTown,
            representative.Location?.Address?.AddressLocality,
            representative.Location?.Address?.AddressPostCode ?? string.Empty,
            country);

        var updatedCommunication = Communication.Create(
            representative.Communication?.Email,
            representative.Communication?.Mobile,
            representative.Communication?.Landline,
            false);

        site.SetLocation(
            representative.LastUpdatedDate,
            representative.Location?.OsMapReference,
            representative.Location?.Easting,
            representative.Location?.Northing,
            updatedAddress,
            [updatedCommunication]);

        site.SetSpecies(species, representative.LastUpdatedDate);

        site.SetActivities(activities, representative.LastUpdatedDate);

        site.SetGroupMarks(groupMarks, representative.LastUpdatedDate);

        site.SetSiteParties(siteParties, representative.LastUpdatedDate);

        return site;
    }

    private static async Task<Country?> GetCountryAsync(
        string? countryIdentifier,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        CancellationToken cancellationToken)
    {
        if (countryIdentifier == null) return null;

        var countryDocument = await getCountryById(countryIdentifier, cancellationToken);

        if (countryDocument == null)
            return null;

        return countryDocument.ToDomain();
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
        if (rawCodes == null)
            return [];

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

    private static List<GroupMark> ToGroupMarks(List<SiteGroupMarkRelationshipDocument> relationships)
    {
        return [.. relationships
            .Where(m => !string.IsNullOrWhiteSpace(m.Herdmark))
            .Select(m =>
            {
                var species = m.SpeciesTypeId is not null
                    ? new Species(
                        id: m.SpeciesTypeId,
                        lastUpdatedDate: m.LastUpdatedDate,
                        code: m.SpeciesTypeCode ?? string.Empty,
                        name: m.SpeciesTypeName ?? string.Empty)
                    : null;

                return new GroupMark(
                    id: m.Id ?? Guid.NewGuid().ToString(),
                    lastUpdatedDate: m.LastUpdatedDate,
                    mark: m.Herdmark,
                    startDate: m.GroupMarkStartDate,
                    endDate: m.GroupMarkEndDate,
                    species: species);
            })];
    }
}