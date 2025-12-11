using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData;

public static class SpeciesData
{
    private static readonly Dictionary<string, SpeciesDocument> s_speciesByCode =
        new()
        {
            ["CTT"] = new SpeciesDocument
            {
                IdentifierId = "5a86d64d-0f17-46a0-92d5-11fd5b2c5830",
                Code = "CTT",
                Name = "Cattle",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            },

            ["SHP"] = new SpeciesDocument
            {
                IdentifierId = "5039a191-fb31-42b7-84e8-852f43ed705c",
                Code = "SHP",
                Name = "Sheep",
                LastModifiedDate = DateTime.UtcNow,
                IsActive = true
            }
        };

    private static readonly Dictionary<string, SpeciesDocument> s_speciesById =
        s_speciesByCode.Values.ToDictionary(c => c.IdentifierId);

    public static (string? id, string? name) Find(string code)
    {
        var type = GetByCode(code);
        if (type == null) return (null, null);
        return (type.IdentifierId, type.Name);
    }

    public static SpeciesDocument GetById(string id) => s_speciesById[id];

    public static SpeciesDocument GetByCode(string code) => s_speciesByCode[code];

    public static IEnumerable<SpeciesDocument> All => s_speciesByCode.Values;

    public static SpeciesSummaryDocument? GetSummary(string code)
    {
        var type = GetByCode(code);
        if (type == null) return null;
        return new SpeciesSummaryDocument
        {
            IdentifierId = type.IdentifierId,
            Code = type.Code,
            Name = type.Name,
            LastModifiedDate = type.LastModifiedDate
        };
    }
}