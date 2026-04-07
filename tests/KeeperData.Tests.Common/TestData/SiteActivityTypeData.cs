using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class SiteActivityTypeData
{
    private static readonly Dictionary<string, SiteActivityTypeDocument> s_siteActivityTypesByCode =
        new()
        {
            ["RM"] = new SiteActivityTypeDocument
            {
                IdentifierId = "d2d9be5e-18b4-4424-b196-fd40f3b105d8",
                Code = "RM",
                Name = "Red Meat",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },

            ["WM"] = new SiteActivityTypeDocument
            {
                IdentifierId = "e0dd8921-3593-4e58-b797-a7c8673d8e40",
                Code = "WM",
                Name = "White Meat",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, SiteActivityTypeDocument> s_siteActivityTypesById =
        s_siteActivityTypesByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? name) Find(string code)
    {
        var type = GetByCode(code);
        if (type == null) return (null, null);
        return (type.IdentifierId, type.Name);
    }

    public static SiteActivityTypeDocument GetById(string id) => s_siteActivityTypesById[id];

    public static SiteActivityTypeDocument GetByCode(string code) => s_siteActivityTypesByCode[code];

    public static IEnumerable<SiteActivityTypeDocument> All => s_siteActivityTypesByCode.Values;
}