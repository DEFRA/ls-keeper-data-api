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
        Func<string?, CancellationToken, Task<PremisesTypeDocument?>> getPremiseTypeById,
        CancellationToken cancellationToken)
    {
        if (silverHoldings?.Count == 0)
            return null;

        var primaryHolding = silverHoldings![0];

        SiteDocument? existingSite = null; // TODO - Inject and lookup via Repository

        var site = existingSite is not null
            ? await UpdateSiteAsync(
                currentDateTime,
                primaryHolding,
                existingSite,
                getCountryById,
                getPremiseTypeById,
                cancellationToken)
            : await CreateSiteAsync(
                currentDateTime,
                primaryHolding,
                getCountryById,
                getPremiseTypeById,
                cancellationToken);

        return SiteDocument.FromDomain(site);
    }

    private static async Task<Site> CreateSiteAsync(
        DateTime currentDateTime,
        SamHoldingDocument incoming,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<PremisesTypeDocument?>> getPremiseTypeById,
        CancellationToken cancellationToken)
    {
        int? uprn = int.TryParse(incoming.Location?.Address?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var premiseType = await GetPremiseTypeAsync(
            incoming.PremiseTypeIdentifier,
            getPremiseTypeById,
            cancellationToken);

        var country = await GetCountryAsync(
            incoming.Location?.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        var address = Address.Create(
            uprn,
            incoming.Location?.Address?.AddressLine ?? string.Empty,
            incoming.Location?.Address?.AddressStreet,
            incoming.Location?.Address?.AddressTown,
            incoming.Location?.Address?.AddressLocality,
            incoming.Location?.Address?.AddressPostCode ?? string.Empty,
            country);

        var location = Location.Create(
            incoming.Location?.OsMapReference,
            incoming.Location?.Easting,
            incoming.Location?.Northing,
            address,
            communication: null);

        var site = Site.Create(
            incoming.LastUpdatedBatchId,
            premiseType?.Code ?? string.Empty,
            incoming.LocationName ?? string.Empty,
            incoming.HoldingStartDate,
            incoming.HoldingEndDate,
            incoming.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            incoming.Deleted,
            location);

        site.AddSiteIdentifier(
            lastUpdatedDate: currentDateTime,
            identifier: incoming.CountyParishHoldingNumber,
            type: HoldingIdentifierType.HoldingNumber.ToString()); // TODO - Need to use LOV

        // TODO - Add additional fields

        return site;
    }

    private static async Task<Site> UpdateSiteAsync(
        DateTime currentDateTime,
        SamHoldingDocument incoming,
        SiteDocument existing,
        Func<string?, CancellationToken, Task<CountryDocument?>> getCountryById,
        Func<string?, CancellationToken, Task<PremisesTypeDocument?>> getPremiseTypeById,
        CancellationToken cancellationToken)
    {
        var site = existing.ToDomain();

        int? uprn = int.TryParse(incoming.Location?.Address?.UniquePropertyReferenceNumber, out var value) ? value : null;

        var premiseType = await GetPremiseTypeAsync(
            incoming.PremiseTypeIdentifier,
            getPremiseTypeById,
            cancellationToken);

        var country = await GetCountryAsync(
            incoming.Location?.Address?.CountryIdentifier,
            getCountryById,
            cancellationToken);

        site.Update(
            currentDateTime,
            incoming.LastUpdatedBatchId,
            premiseType?.Code ?? string.Empty,
            incoming.LocationName ?? string.Empty,
            incoming.HoldingStartDate,
            incoming.HoldingEndDate,
            incoming.HoldingStatus,
            SourceSystemType.SAM.ToString(),
            null,
            incoming.Deleted);

        var updatedAddress = Address.Create(
            uprn,
            incoming.Location?.Address?.AddressLine ?? string.Empty,
            incoming.Location?.Address?.AddressStreet,
            incoming.Location?.Address?.AddressTown,
            incoming.Location?.Address?.AddressLocality,
            incoming.Location?.Address?.AddressPostCode ?? string.Empty,
            country);

        site.UpdateLocation(
            currentDateTime,
            incoming.Location?.OsMapReference,
            incoming.Location?.Easting,
            incoming.Location?.Northing,
            updatedAddress,
            null);

        // TODO - Add additional fields

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
}