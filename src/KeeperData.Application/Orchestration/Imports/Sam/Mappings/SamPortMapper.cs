using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Extensions;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamPortMapper
{
    private const string PortSiteTypeCode = "PO";
    private const string PortBusinessUsage = "Port";
    public static async Task<List<SamHoldingDocument>> ToSilver(
        List<SamPort> rawPorts,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        var result = new List<SamHoldingDocument>();

        foreach (var p in rawPorts?.Where(x => !string.IsNullOrWhiteSpace(x.CPH)) ?? [])
        {
            var holding = await ToSilver(
                p,
                resolveCountry,
                cancellationToken);

            result.Add(holding);
        }

        return result;
    }

    public static async Task<SamHoldingDocument> ToSilver(
        SamPort p,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        // Ports don't have country information in the raw data, so we default to null
        var (countryId, countryCode, _) = await resolveCountry(null, null, cancellationToken);

        var result = new SamHoldingDocument
        {
            // Id - Leave to support upsert assigning Id

            LastUpdatedBatchId = p.BATCH_ID,
            CreatedDate = p.CreatedAtUtc ?? DateTime.UtcNow,
            LastUpdatedDate = p.UpdatedAtUtc ?? DateTime.UtcNow,
            Deleted = p.IsDeleted ?? false,

            CountyParishHoldingNumber = p.CPH,
            AlternativeHoldingIdentifier = null,

            CphRelationshipType = null,
            SecondaryCph = null,

            CphTypeIdentifier = HoldingIdentifierType.PRTN.ToString(),
            LocationName = p.PREMISES_NAME,

            DiseaseType = null,
            Interval = null,
            IntervalUnitOfTime = null,

            HoldingStartDate = p.CreatedAtUtc ?? DateTime.UtcNow,
            HoldingEndDate = null,
            HoldingStatus = HoldingStatusFormatters.FormatHoldingStatus(p.IsDeleted ?? false),

            MovementRestrictionReasonCode = null,

            SourceFacilityTypeCode = null,
            SourceFacilityBusinessActivityCode = null,
            SourceFacilitySubBusinessActivityCode = PortBusinessUsage,

            SiteActivityTypeId = null,
            SiteActivityTypeCode = null,

            SiteTypeIdentifier = null,
            SiteTypeCode = PortSiteTypeCode,

            SpeciesTypeCode = null,
            ProductionUsageCodeList = [],

            Location = new Core.Documents.Silver.LocationDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Easting = p.EASTING,
                Northing = p.NORTHING,
                OsMapReference = p.MAP_REFERENCE,
                Address = new Core.Documents.Silver.AddressDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    AddressLine = p.ADDRESS_LINE_1,
                    AddressLocality = p.ADDRESS_LINE_2,
                    AddressStreet = p.ADDRESS_LINE_3,
                    AddressTown = null,
                    AddressPostCode = p.POSTCODE,
                    CountrySubDivision = null,

                    CountryIdentifier = countryId,
                    CountryCode = countryCode,

                    UniquePropertyReferenceNumber = null
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
}