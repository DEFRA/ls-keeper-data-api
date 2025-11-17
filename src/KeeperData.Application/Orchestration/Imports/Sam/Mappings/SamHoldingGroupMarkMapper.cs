using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamHoldingGroupMarkMapper
{
    public static List<SamHoldingDocument> EnrichHoldingsWithGroupMarks(
        List<SamHoldingDocument>? silverHoldings,
        List<SamHerdDocument>? silverHerds)
    {
        if (silverHoldings is null || silverHoldings.Count == 0)
            return [];

        if (silverHerds is null || silverHerds.Count == 0)
            return silverHoldings;

        var herdsByCph = silverHerds
            .Where(h => !string.IsNullOrWhiteSpace(h.CountyParishHoldingHerd))
            .GroupBy(h => h.CountyParishHoldingHerd)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var holding in silverHoldings)
        {
            if (string.IsNullOrWhiteSpace(holding.CountyParishHoldingNumber))
                continue;

            if (!herdsByCph.TryGetValue(holding.CountyParishHoldingNumber, out var matchingHerds))
                continue;

            var groupMarks = matchingHerds.Select(herd => new GroupMarkDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),

                GroupMark = herd.Herdmark,
                CountyParishHoldingNumber = herd.CountyParishHoldingNumber,

                GroupMarkStartDate = herd.GroupMarkStartDate,
                GroupMarkEndDate = herd.GroupMarkEndDate,

                SpeciesTypeId = herd.SpeciesTypeId,
                SpeciesTypeCode = herd.SpeciesTypeCode,

                ProductionUsageId = herd.ProductionUsageId,
                ProductionUsageCode = herd.ProductionUsageCode,

                ProductionTypeId = herd.ProductionTypeId,
                ProductionTypeCode = herd.ProductionTypeCode,

                TbTestingIntervalId = TestingIntervalFormatters.FormatTbTestingInterval(
                    herd.Interval,
                    herd.IntervalUnitOfTime)
            }).ToList();

            holding.GroupMarks = groupMarks;
        }

        return silverHoldings;
    }
}