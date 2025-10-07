using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Deletions.Steps;

[StepOrder(1)]
public class CtsHoldingDeletePersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    ILogger<CtsHoldingDeletePersistenceStep> logger)
    : ImportStepBase<CtsHoldingDeleteContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingDeleteContext context, CancellationToken cancellationToken)
    {
        // Find silver holding > mark as deleted
        var silverHoldingResults = await _silverHoldingRepository
            .FindAsync(x => x.CountyParishHoldingNumber.Equals(context.Cph, StringComparison.InvariantCultureIgnoreCase),
            cancellationToken);

        context.SilverHoldings = [];

        foreach (var silver in silverHoldingResults ?? [])
        {
            silver.Deleted = true;
            context.SilverHoldings.Add(silver);
        }

        if (context.SilverHoldings.Count != 0)
            await _silverHoldingRepository.BulkUpsertAsync(context.SilverHoldings, cancellationToken);

        await Task.CompletedTask;
    }
}