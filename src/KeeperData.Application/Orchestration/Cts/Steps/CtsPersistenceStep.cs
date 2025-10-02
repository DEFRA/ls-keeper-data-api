using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Source;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Steps;

[StepOrder(4)]
public class CtsPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsPersistenceStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHolding is not null)
            await _silverHoldingRepository.BulkUpsertAsync([context.SilverHolding], cancellationToken);

        if (context.SilverParties is not null)
            await _silverPartyRepository.BulkUpsertAsync(context.SilverParties, cancellationToken);

        await Task.CompletedTask;
    }
}