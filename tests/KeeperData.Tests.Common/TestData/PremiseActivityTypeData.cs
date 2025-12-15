using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class PremiseActivityTypeData
{
    private static readonly Dictionary<string, PremisesActivityTypeDocument> s_premiseActivityTypesByCode =
        new()
        {
            ["RM"] = new PremisesActivityTypeDocument
            {
                IdentifierId = "d2d9be5e-18b4-4424-b196-fd40f3b105d8",
                Code = "RM",
                Name = "Red Meat",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },

            ["WM"] = new PremisesActivityTypeDocument
            {
                IdentifierId = "e0dd8921-3593-4e58-b797-a7c8673d8e40",
                Code = "WM",
                Name = "White Meat",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, PremisesActivityTypeDocument> s_premiseActivityTypesById =
        s_premiseActivityTypesByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? name) Find(string code)
    {
        var type = GetByCode(code);
        if (type == null) return (null, null);
        return (type.IdentifierId, type.Name);
    }

    public static PremisesActivityTypeDocument GetById(string id) => s_premiseActivityTypesById[id];

    public static PremisesActivityTypeDocument GetByCode(string code) => s_premiseActivityTypesByCode[code];

    public static IEnumerable<PremisesActivityTypeDocument> All => s_premiseActivityTypesByCode.Values;
}