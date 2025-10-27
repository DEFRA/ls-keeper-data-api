using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamHoldingMapper
{
    private const string SaonLabel = "";

    public static async Task<List<SamHoldingDocument>> ToSilver(
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

                Location = new LocationDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Easting = h.EASTING,
                    Northing = h.NORTHING,
                    OsMapReference = h.OS_MAP_REFERENCE,
                    Address = new AddressDocument
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

                Communication = new CommunicationDocument
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
}