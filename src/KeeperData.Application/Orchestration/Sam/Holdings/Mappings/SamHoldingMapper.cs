using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamHoldingMapper
{
    private const string SaonLabel = "";

    public static async Task<List<SamHoldingDocument>> ToSilver(
        DateTime currentDateTime,
        List<SamCphHolding> rawHoldings,
        Func<string?, CancellationToken, Task<(string? PremiseActivityTypeId, string? PremiseActivityTypeName)>> resolvePremiseActivityType,
        Func<string?, CancellationToken, Task<(string? PremiseTypeId, string? PremiseTypeName)>> resolvePremiseType,
        Func<string?, CancellationToken, Task<(string? CountryId, string? CountryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamHoldingDocument>();

        foreach (var h in rawHoldings?.Where(x => x.CPH != null) ?? [])
        {
            var addressLine = AddressFormatters.FormatAddressRange(
                            h.SAON_START_NUMBER, h.SAON_START_NUMBER_SUFFIX,
                            h.SAON_END_NUMBER, h.SAON_END_NUMBER_SUFFIX,
                            h.PAON_START_NUMBER, h.PAON_START_NUMBER_SUFFIX,
                            h.PAON_END_NUMBER, h.PAON_END_NUMBER_SUFFIX,
                            saonLabel: SaonLabel);

            var (premiseActivityTypeId, premiseActivityTypeName) = await resolvePremiseActivityType(h.FACILITY_BUSINSS_ACTVTY_CODE, cancellationToken);
            var (premiseTypeId, premiseTypeName) = await resolvePremiseType(h.FACILITY_TYPE_CODE, cancellationToken);
            var (countryId, countryName) = await resolveCountry(h.COUNTRY_CODE, cancellationToken);

            var holder = new SamHoldingDocument
            {
                // Id - Leave to support upsert assigning Id

                LastUpdatedBatchId = h.BATCH_ID,
                LastUpdatedDate = currentDateTime,
                Deleted = h.IsDeleted ?? false,

                CountyParishHoldingNumber = h.CPH,
                AlternativeHoldingIdentifier = null,

                CphTypeIdentifier = h.CPH_TYPE,
                LocationName = h.FEATURE_NAME,

                HoldingStartDate = h.FEATURE_ADDRESS_FROM_DATE,
                HoldingEndDate = h.FEATURE_ADDRESS_TO_DATE,
                HoldingStatus = h.FEATURE_ADDRESS_TO_DATE.HasValue
                                    && h.FEATURE_ADDRESS_TO_DATE != default
                                    ? HoldingStatusType.Inactive.ToString()
                                    : HoldingStatusType.Active.ToString(),

                PremiseActivityTypeId = premiseActivityTypeId,
                PremiseActivityTypeCode = h.FACILITY_BUSINSS_ACTVTY_CODE,

                PremiseTypeIdentifier = premiseTypeId,
                PremiseTypeCode = h.FACILITY_TYPE_CODE,

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

            result.Add(holder);
        }

        return result;
    }

    public static async Task<SiteDocument?> ToGold(
        DateTime currentDateTime,
        List<SamHoldingDocument> silverHoldings,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<PremiseTypeDocument?>> getPremiseTypeById,
        CancellationToken cancellationToken)
    {
        if (silverHoldings?.Count == 0)
            return null;

        var primaryHolding = silverHoldings![0];

        // Find in DB
        SiteDocument? exisingSite = null;

        // Exists? Update
        if (exisingSite != null)
        {
            var updatedSite = await UpdateSiteAsync(
                currentDateTime,
                primaryHolding,
                exisingSite,
                getCountryById,
                cancellationToken);

            return SiteDocument.FromDomain(updatedSite);
        }

        // Not Exists? Create
        var newSite = await CreateSiteAsync(
            currentDateTime,
            primaryHolding,
            getCountryById,
            getPremiseTypeById,
            cancellationToken);

        return SiteDocument.FromDomain(newSite);
    }

    private static async Task<Site> CreateSiteAsync(
        DateTime currentDateTime,
        SamHoldingDocument incoming,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<PremiseTypeDocument?>> getPremiseTypeById,
        CancellationToken cancellationToken)
    {
        int? addressUprn = int.TryParse(incoming.Location?.Address?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var sitePremiseType = await GetPremiseTypeAsync(
            incoming.PremiseTypeIdentifier,
            getPremiseTypeById,
            cancellationToken);

        var siteAddressCountry = await GetCountryAsync(
            incoming.Location?.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var siteAddress = Address.Create(
            uprn: addressUprn,
            addressLine1: incoming.Location?.Address?.AddressLine ?? string.Empty,
            addressLine2: incoming.Location?.Address?.AddressStreet,
            postTown: incoming.Location?.Address?.AddressTown,
            county: incoming.Location?.Address?.AddressLocality,
            postCode: incoming.Location?.Address?.AddressPostCode ?? string.Empty,
            country: siteAddressCountry);

        var siteLocation = Location.Create(
            osMapReference: incoming.Location?.OsMapReference,
            easting: incoming.Location?.Easting,
            northing: incoming.Location?.Northing,
            address: siteAddress,
            communication: null);

        var site = Site.Create(
            batchId: incoming.LastUpdatedBatchId,
            lastUpdatedDate: currentDateTime,
            type: sitePremiseType?.Code ?? string.Empty,
            name: incoming.LocationName ?? string.Empty,
            startDate: incoming.HoldingStartDate,
            endDate: incoming.HoldingEndDate,
            state: incoming.HoldingStatus,
            source: SourceSystemType.SAM.ToString(),
            destroyIdentityDocumentsFlag: null,
            deleted: incoming.Deleted,
            location: siteLocation);

        site.AddSiteIdentifier(
            lastUpdatedDate: currentDateTime,
            identifier: incoming.CountyParishHoldingNumber,
            type: HoldingIdentifierType.HoldingNumber.ToString());

        return site;
    }

    private static async Task<Site> UpdateSiteAsync(
        DateTime currentDateTime,
        SamHoldingDocument incoming,
        SiteDocument existing,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        CancellationToken cancellationToken)
    {
        var site = existing.ToDomain();

        // TODO

        return await Task.FromResult(site);
    }

    private static async Task<Country?> GetCountryAsync(
        string? countryIdentifier,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        CancellationToken cancellationToken)
    {
        var countryDocument = await getCountryById(countryIdentifier, cancellationToken);

        if (countryDocument == null)
            return null;

        return countryDocument.ToDomain();
    }

    private static async Task<PremiseTypeDocument?> GetPremiseTypeAsync(
        string? premiseTypeIdentifier,
        Func<string?, CancellationToken, Task<PremiseTypeDocument?>> getPremiseTypeById,
        CancellationToken cancellationToken)
    {
        return await getPremiseTypeById(premiseTypeIdentifier, cancellationToken);
    }
}