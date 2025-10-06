using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(4)]
public class CtsHoldingInsertedPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsHoldingInsertedPersistenceStep> logger)
    : ImportStepBase<CtsHoldingInsertedContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingInsertedContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHolding is not null)
            await _silverHoldingRepository.BulkUpsertAsync([context.SilverHolding], cancellationToken);

        if (context.SilverParties is not null)
            await _silverPartyRepository.BulkUpsertAsync(context.SilverParties, cancellationToken);

        await Task.CompletedTask;
    }
}