using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class CountryData
{
    public const string CountryIdAsGb = "5e4b8d0d-96a8-4102-81e2-f067ee85d030";
    public const string CountryCodeAsGb = "GB";
    public const string CountryNameAsGb = "United Kingdom";

    private static readonly Dictionary<string, CountryDocument> s_countriesByCode =
        new()
        {
            ["GB"] = new CountryDocument
            {
                IdentifierId = "5e4b8d0d-96a8-4102-81e2-f067ee85d030",
                Code = "GB",
                Name = "United Kingdom",
                LongName = "United Kingdom of Great Britain and Northern Ireland",
                DevolvedAuthority = false,
                EuTradeMember = false,
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },
            ["GB-ENG"] = new CountryDocument
            {
                IdentifierId = "3c04912b-88dc-42cf-a6f0-ac9cbed11c3b",
                Code = "GB-ENG",
                Name = "England",
                LongName = "England - United Kingdom",
                DevolvedAuthority = true,
                EuTradeMember = false,
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, CountryDocument> s_countriesById =
        s_countriesByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? code, string? name) Find(string code, string? internalCode)
    {
        var searchCode = DetermineSearchKey(code, internalCode);
        var type = GetByCode(searchCode!);
        if (type == null) return (null, null, null);
        return (type.IdentifierId, type.Code, type.Name);
    }

    public static CountryDocument GetById(string id) => s_countriesById[id];

    public static CountryDocument GetByCode(string code) => s_countriesByCode[code];

    public static IEnumerable<CountryDocument> All => s_countriesByCode.Values;

    public static CountrySummaryDocument? GetSummary(string code)
    {
        var type = GetByCode(code);
        if (type == null) return null;
        return new CountrySummaryDocument
        {
            IdentifierId = type.IdentifierId,
            Code = type.Code,
            Name = type.Name,
            LongName = type.LongName,
            DevolvedAuthorityFlag = type.DevolvedAuthority,
            EuTradeMemberFlag = type.EuTradeMember,
            LastModifiedDate = type.LastModifiedDate
        };
    }

    private static string? DetermineSearchKey(string? countryCode, string? ukInternalCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return null;

        if (string.Equals(countryCode, "GB", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(ukInternalCode))
        {
            return ukInternalCode.ToUpperInvariant().Trim() switch
            {
                "ENGLAND" => "GB-ENG",
                "WALES" => "GB-WLS",
                "SCOTLAND" => "GB-SCT",
                "NORTHERN IRELAND" => "GB-NIR",
                _ => countryCode
            };
        }

        return countryCode;
    }
}