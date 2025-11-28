using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class PremiseTypeData
{
    private static readonly Dictionary<string, PremisesTypeDocument> s_premiseTypesByCode =
        new()
        {
            ["AH"] = new PremisesTypeDocument
            {
                IdentifierId = "d819dc18-f5a1-4d1a-b332-d18f9d1f9227",
                Code = "AH",
                Name = "Agricultural Holding",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, PremisesTypeDocument> s_premiseTypesById =
        s_premiseTypesByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? name) Find(string code)
    {
        var type = GetByCode(code);
        if (type == null) return (null, null);
        return (type.IdentifierId, type.Name);
    }

    public static PremisesTypeDocument GetById(string id) => s_premiseTypesById[id];

    public static PremisesTypeDocument GetByCode(string code) => s_premiseTypesByCode[code];

    public static IEnumerable<PremisesTypeDocument> All => s_premiseTypesByCode.Values;
}
