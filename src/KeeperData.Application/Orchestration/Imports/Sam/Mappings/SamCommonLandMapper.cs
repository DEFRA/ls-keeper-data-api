using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using System.Globalization;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamCommonLandMapper
{
    private const string CommonLandSiteTypeCode = "CL";
    private const string CommonLandBusinessUsage = "Common Land";

    public static async Task<List<SamHoldingDocument>> ToSilver(
        List<SamCommonLand> rawCommonLands,
        Func<string?, string?, CancellationToken, Task<(string? countryId, string? countryCode, string? countryName)>> resolveCountry,
        CancellationToken cancellationToken)
    {
        if (rawCommonLands == null || rawCommonLands.Count == 0)
            return [];

        var result = new List<SamHoldingDocument>();

        var groups = rawCommonLands
            .Where(r => !string.IsNullOrWhiteSpace(r.COMMON_CPH))
            .GroupBy(r => r.COMMON_CPH);

        foreach (var group in groups)
        {
            var commonCph = group.Key!;

            // Use the most recently updated row as the source for address and metadata.
            // All rows in the group share the same physical common land, so the address
            // fields are equivalent; this just ensures we use the freshest record.
            var representative = group
                .OrderByDescending(r => r.UpdatedAtUtc ?? DateTime.MinValue)
                .First();

            // Collect every main-CPH relationship from all rows in the group into one list.
            var associatedMainHoldings = group
                .Where(r => r.IsMainCphPopulated)
                .Select(r => new AssociatedHoldingRelationship
                {
                    HoldingIdentifier = r.MAIN_CPH,
                    ContiguousFlag = string.Equals(r.CONTIGUOUS_COMMON, "Yes", StringComparison.OrdinalIgnoreCase),
                    StartDate = NormaliseDate(r.START_DATE),
                    EndDate = NormaliseDate(r.END_DATE)
                })
                .ToList();

            var (countryId, countryCode, _) = await resolveCountry(representative.COUNTRY, null, cancellationToken);

            var holding = new SamHoldingDocument
            {
                LastUpdatedBatchId = representative.BATCH_ID,
                CreatedDate = representative.CreatedAtUtc ?? DateTime.UtcNow,
                LastUpdatedDate = representative.UpdatedAtUtc ?? DateTime.UtcNow,
                Deleted = representative.IsDeleted ?? false,

                IsFromCommonLandSource = true,
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
                        AddressStreet = representative.ADDRESS_LINE_2,
                        AddressTown = representative.ADDRESS_LINE_3,
                        AddressPostCode = representative.POSTCODE,
                        CountryCode = countryCode,
                        CountryIdentifier = countryId
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