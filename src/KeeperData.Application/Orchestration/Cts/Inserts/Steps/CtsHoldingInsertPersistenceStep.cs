using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(4)]
public class CtsHoldingInsertPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsHoldingInsertPersistenceStep> logger)
    : ImportStepBase<CtsHoldingInsertContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingInsertContext context, CancellationToken cancellationToken)
    {
        //if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeInsert })
        //    return;

        //if (context.SilverHoldings is not null)
        //    await _silverHoldingRepository.BulkUpsertAsync(context.SilverHoldings, cancellationToken);

        //if (context.SilverParties is not null)
        //    await _silverPartyRepository.BulkUpsertAsync(context.SilverParties, cancellationToken);

        await Task.CompletedTask;
    }
}