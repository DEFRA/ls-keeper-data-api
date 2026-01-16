using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamHerdMapper
{
    public static async Task<List<SamHerdDocument>> ToSilver(
        List<SamHerd> rawHerds,
        Func<string?, CancellationToken, Task<(string? ProductionUsageId, string? ProductionUsageName)>> resolveProductionUsage,
        Func<string?, CancellationToken, Task<(string? SpeciesTypeId, string? SpeciesTypeName)>> resolveSpeciesType,
        CancellationToken cancellationToken)
    {
        var result = new List<SamHerdDocument>();

        foreach (var h in rawHerds?.Where(x => x.CPHH != null) ?? [])
        {
            var herd = await ToSilver(
                h,
                resolveProductionUsage,
                resolveSpeciesType,
                cancellationToken);

            result.Add(herd);
        }

        return result;
    }

    public static async Task<SamHerdDocument> ToSilver(
        SamHerd h,
        Func<string?, CancellationToken, Task<(string? ProductionUsageId, string? ProductionUsageName)>> resolveProductionUsage,
        Func<string?, CancellationToken, Task<(string? SpeciesTypeId, string? SpeciesTypeName)>> resolveSpeciesType,
        CancellationToken cancellationToken)
    {
        var formattedProductionUsageCode = ProductionUsageCodeFormatters.TrimProductionUsageCodeHerd(h.AnimalPurposeCodeUnwrapped);
        var (speciesTypeId, speciesTypeName) = await resolveSpeciesType(h.AnimalSpeciesCodeUnwrapped, cancellationToken);
        var (productionUsageId, _) = await resolveProductionUsage(formattedProductionUsageCode, cancellationToken);

        var result = new SamHerdDocument
        {
            // Id - Leave to support upsert assigning Id

            LastUpdatedBatchId = h.BATCH_ID,
            CreatedDate = h.CreatedAtUtc ?? DateTime.UtcNow,
            LastUpdatedDate = h.UpdatedAtUtc ?? DateTime.UtcNow,
            Deleted = h.IsDeleted ?? false,

            Herdmark = h.HERDMARK,
            CountyParishHoldingHerd = h.CPHH,
            CountyParishHoldingNumber = h.CPHH.CphhToCph(),

            SpeciesTypeId = speciesTypeId,
            SpeciesTypeCode = h.AnimalSpeciesCodeUnwrapped,
            SpeciesTypeName = speciesTypeName,

            ProductionUsageId = productionUsageId,
            ProductionUsageCode = formattedProductionUsageCode,
            AnimalPurposeCode = h.AnimalPurposeCodeUnwrapped,

            ProductionTypeId = null,
            ProductionTypeCode = null,

            DiseaseType = h.DISEASE_TYPE,
            Interval = h.INTERVALS,
            IntervalUnitOfTime = h.INTERVAL_UNIT_OF_TIME,

            MovementRestrictionReasonCode = h.MOVEMENT_RSTRCTN_RSN_CODE,

            GroupMarkStartDate = h.ANIMAL_GROUP_ID_MCH_FRM_DAT,
            GroupMarkEndDate = h.ANIMAL_GROUP_ID_MCH_TO_DAT,

            KeeperPartyIdList = h.KeeperPartyIdList,
            OwnerPartyIdList = h.OwnerPartyIdList
        };

        return result;
    }
}