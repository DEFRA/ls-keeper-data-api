using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamHerdMapper
{
    public static async Task<List<SamHerdDocument>> ToSilver(
        DateTime currentDateTime,
        List<SamHerd> rawHerds,
        Func<string?, CancellationToken, Task<(string? ProductionUsageId, string? ProductionUsageName)>> resolveProductionUsage,
        // Func<string?, CancellationToken, Task<(string? ProductionTypeId, string? ProductionTypeName)>> resolveProductionType,
        Func<string?, CancellationToken, Task<(string? SpeciesTypeId, string? SpeciesTypeName)>> resolveSpeciesType,
        CancellationToken cancellationToken)
    {
        var result = new List<SamHerdDocument>();

        foreach (var h in rawHerds?.Where(x => x.CPHH != null) ?? [])
        {
            var (speciesTypeId, speciesTypeCode) = await resolveSpeciesType(h.AnimalSpeciesCodeUnwrapped, cancellationToken);
            var (productionUsageId, productionUsageCode) = await resolveProductionUsage(h.AnimalPurposeCodeUnwrapped, cancellationToken);

            // TODO - Mapped from where
            // var (productionTypeId, productionTypeCode) = await resolveProductionType(herd.TBC, cancellationToken);

            var herd = new SamHerdDocument
            {
                // Id - Leave to support upsert assigning Id

                LastUpdatedBatchId = h.BATCH_ID,
                LastUpdatedDate = currentDateTime,
                Deleted = h.IsDeleted ?? false,

                Herdmark = h.HERDMARK,
                CountyParishHoldingHerd = h.CPHH,
                CountyParishHoldingNumber = h.CPHH.CphhToCph(),

                SpeciesTypeId = speciesTypeId,
                SpeciesTypeCode = h.AnimalSpeciesCodeUnwrapped,

                ProductionUsageId = productionUsageId,
                ProductionUsageCode = h.AnimalPurposeCodeUnwrapped,

                // TODO - Mapped from where
                // ProductionTypeId = productionTypeId,
                // ProductionTypeCode = herd.TBC,

                DiseaseType = h.DISEASE_TYPE,
                Interval = h.INTERVAL,
                IntervalUnitOfTime = h.INTERVAL_UNIT_OF_TIME,

                MovementRestrictionReasonCode = h.MOVEMENT_RSTRCTN_RSN_CODE,

                GroupMarkStartDate = h.ANIMAL_GROUP_ID_MCH_FRM_DAT,
                GroupMarkEndDate = h.ANIMAL_GROUP_ID_MCH_TO_DAT,

                KeeperPartyIdList = h.KeeperPartyIdList,
                OwnerPartyIdList = h.OwnerPartyIdList
            };

            result.Add(herd);
        }

        return result;
    }
}