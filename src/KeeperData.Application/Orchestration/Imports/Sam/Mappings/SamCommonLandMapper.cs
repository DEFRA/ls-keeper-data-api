using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamCommonLandMapper
{
    private const string CommonLandSiteTypeCode = "CL";

    public static List<SamHoldingDocument> ToSilver(List<SamCommonLand> rawCommonLands)
    {
        if (rawCommonLands == null || rawCommonLands.Count == 0)
            return [];

        var definitionRecords = rawCommonLands
            .Where(r => r.IsDefinitionRecord && !string.IsNullOrWhiteSpace(r.COMMON_CPH))
            .GroupBy(r => r.COMMON_CPH)
            .ToList();

        var relationshipRecords = rawCommonLands
            .Where(r => r.IsRelationshipRecord)
            .ToList();

        var result = new List<SamHoldingDocument>();

        foreach (var group in definitionRecords)
        {
            var representative = group.OrderByDescending(r => r.UpdatedAtUtc).First();
            var commonCph = group.Key;

            var associatedMainHoldings = relationshipRecords
                .Where(r => r.COMMON_CPH == commonCph)
                .Select(r => new AssociatedHoldingRelationship
                {
                    HoldingIdentifier = r.MAIN_CPH,
                    ContiguousFlag = string.Equals(r.CONTIGUOUS_COMMON, "Yes", StringComparison.OrdinalIgnoreCase),
                    StartDate = NormaliseDate(r.START_DATE),
                    EndDate = NormaliseDate(r.END_DATE)
                })
                .ToList();

            var holding = new SamHoldingDocument
            {
                LastUpdatedBatchId = representative.BATCH_ID,
                CreatedDate = representative.CreatedAtUtc ?? DateTime.UtcNow,
                LastUpdatedDate = representative.UpdatedAtUtc ?? DateTime.UtcNow,
                Deleted = representative.IsDeleted ?? false,

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

    public static List<AssociatedHoldingRelationship> ToAssociatedCommonLands(List<SamCommonLand> relationshipRecords)
    {
        return relationshipRecords
            .Where(r => r.IsRelationshipRecord && !string.IsNullOrWhiteSpace(r.COMMON_CPH))
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

        if (DateTime.TryParse(date, out var parsed))
        {
            if (parsed.Year >= 2999)
                return null;

            return parsed.ToString("yyyy-MM-dd");
        }

        return date;
    }

    private static string? UnwrapPlaceholder(string? value)
        => string.IsNullOrWhiteSpace(value) || value == "-" ? null : value;

    private static double? ParseNullableDouble(string? value)
        => double.TryParse(value, out var result) ? result : null;
}
