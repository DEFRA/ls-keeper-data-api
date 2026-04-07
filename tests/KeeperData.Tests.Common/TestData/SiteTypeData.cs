using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class SiteTypeData
{
    private static readonly Dictionary<string, SiteTypeDocument> s_siteTypesByCode =
        new()
        {
            ["AH"] = new SiteTypeDocument
            {
                IdentifierId = "d819dc18-f5a1-4d1a-b332-d18f9d1f9227",
                Code = "AH",
                Name = "Agricultural Holding",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, SiteTypeDocument> s_siteTypesById =
        s_siteTypesByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? name) Find(string code)
    {
        var type = GetByCode(code);
        if (type == null) return (null, null);
        return (type.IdentifierId, type.Name);
    }

    public static SiteTypeDocument GetById(string id) => s_siteTypesById[id];

    public static SiteTypeDocument GetByCode(string code) => s_siteTypesByCode[code];

    public static IEnumerable<SiteTypeDocument> All => s_siteTypesByCode.Values;
}