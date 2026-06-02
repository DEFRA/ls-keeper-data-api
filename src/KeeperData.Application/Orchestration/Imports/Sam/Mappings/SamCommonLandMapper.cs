using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using System.Globalization;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamCommonLandMapper
{
    private const string CommonLandSiteTypeCode = "CL";
    private const string CommonLandBusinessUsage = "Common Land";

    public static List<SamHoldingDocument> ToSilver(List<SamCommonLand> rawCommonLands)
    {
        if (rawCommonLands == null || rawCommonLands.Count == 0)
            return [];

        var result = new List<SamHoldingDocument>();

        foreach (var representative in rawCommonLands.Where(r => !string.IsNullOrWhiteSpace(r.COMMON_CPH)))
        {
            var commonCph = representative.COMMON_CPH!;

            var associatedMainHoldings = new List<AssociatedHoldingRelationship>();
            if (representative.IsMainCphPopulated)
            {
                associatedMainHoldings.Add(new AssociatedHoldingRelationship
                {
                    HoldingIdentifier = representative.MAIN_CPH,
                    ContiguousFlag = string.Equals(representative.CONTIGUOUS_COMMON, "Yes", StringComparison.OrdinalIgnoreCase),
                    StartDate = NormaliseDate(representative.START_DATE),
                    EndDate = NormaliseDate(representative.END_DATE)
                });
            }

            var holding = new SamHoldingDocument
            {
                LastUpdatedBatchId = representative.BATCH_ID,
                CreatedDate = representative.CreatedAtUtc ?? DateTime.UtcNow,
                LastUpdatedDate = representative.UpdatedAtUtc ?? DateTime.UtcNow,
                Deleted = representative.IsDeleted ?? false,

                SourceFacilitySubBusinessActivityCode = CommonLandBusinessUsage,

                CountyParishHoldingNumber = commonCph,
                CphTypeIdentifier = string.Empty,
                LocationName = UnwrapPlaceholder(representative.PREMISES_NAME),

                HoldingStartDate = default,
                HoldingEndDate = null,
                HoldingStatus = HoldingStatusFormatters.FormatHoldingStatus(representative.IsDeleted ?? false),

                SiteTypeCode = CommonLandSiteTypeCode,

                LocalAuthorityName = representative.LOCAL_AUTH_NAME,
                AssociatedMainHoldings = associatedMainHoldings,

                Location = new LocationDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Easting = ParseNullableDouble(representative.EASTING),
                    Northing = ParseNullableDouble(representative.NORTHING),
                    Address = new AddressDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        AddressLine = representative.ADDRESS_LINE_1,
                        AddressLocality = representative.ADDRESS_LINE_2,
                        AddressStreet = representative.ADDRESS_LINE_3,
                        AddressPostCode = representative.POSTCODE,
                        CountryCode = representative.COUNTRY
                    }
                }
            };

            result.Add(holding);
        }

        return result;
    }

    public static List<AssociatedHoldingRelationship> ToAssociatedCommonLands(List<SamCommonLand>? relationshipRecords)
    {
        if (relationshipRecords == null || relationshipRecords.Count == 0)
            return [];

        return relationshipRecords
            .Where(r => !string.IsNullOrWhiteSpace(r.COMMON_CPH))
            .Select(r => new AssociatedHoldingRelationship
            {
                HoldingIdentifier = r.COMMON_CPH,
                ContiguousFlag = string.Equals(r.CONTIGUOUS_COMMON, "Yes", StringComparison.OrdinalIgnoreCase),
                StartDate = NormaliseDate(r.START_DATE),
                EndDate = NormaliseDate(r.END_DATE)
            })
            .ToList();
    }

    private static string? NormaliseDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            if (parsed.Year >= 2999)
                return null;

            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return date;
    }

    private static string? UnwrapPlaceholder(string? value)
        => string.IsNullOrWhiteSpace(value) || value == "-" ? null : value;

    private static double? ParseNullableDouble(string? value)
        => double.TryParse(value, out var result) ? result : null;
}