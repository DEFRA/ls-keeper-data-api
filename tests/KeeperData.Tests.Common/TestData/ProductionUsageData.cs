using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class ProductionUsageData
{
    private static readonly Dictionary<string, ProductionUsageDocument> s_productionUsageByCode =
        new()
        {
            ["BEEF"] = new ProductionUsageDocument
            {
                IdentifierId = "ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824",
                Code = "BEEF",
                Description = "Beef",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },

            ["MEAT"] = new ProductionUsageDocument
            {
                IdentifierId = "9c517167-c431-4f72-9849-addb5fff118e",
                Code = "MEAT",
                Description = "Meat",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, ProductionUsageDocument> s_productionUsageById =
        s_productionUsageByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? name) Find(string code)
    {
        var type = GetByCode(code);
        if (type == null) return (null, null);
        return (type.IdentifierId, type.Description);
    }

    public static ProductionUsageDocument GetById(string id) => s_productionUsageById[id];

    public static ProductionUsageDocument GetByCode(string code) => s_productionUsageByCode[code];

    public static IEnumerable<ProductionUsageDocument> All => s_productionUsageByCode.Values;
}