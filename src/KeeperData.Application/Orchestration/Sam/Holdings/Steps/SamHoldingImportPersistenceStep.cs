using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Steps;

[StepOrder(4)]
public class SamHoldingImportPersistenceStep(
    IGenericRepository<SamHoldingDocument> silverHoldingRepository,
    IGenericRepository<SamPartyDocument> silverPartyRepository,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamHoldingImportPersistenceStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    private readonly IGenericRepository<SamHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<SamPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<SiteDocument> _goldSiteRepository = goldSiteRepository;
    private readonly IGenericRepository<PartyDocument> _goldPartyRepository = goldPartyRepository;

    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        //if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeInsert })
        //    return;

        //if (context.SilverHolding is not null)
        //    await _silverHoldingRepository.BulkUpsertAsync([context.SilverHolding], cancellationToken);

        //if (context.SilverParties is not null)
        //    await _silverPartyRepository.BulkUpsertAsync(context.SilverParties, cancellationToken);

        //if (context.GoldSite is not null)
        //    await _goldSiteRepository.BulkUpsertAsync([context.GoldSite], cancellationToken);

        //if (context.GoldParties is not null)
        //    await _goldPartyRepository.BulkUpsertAsync(context.GoldParties, cancellationToken);

        await Task.CompletedTask;
    }
}