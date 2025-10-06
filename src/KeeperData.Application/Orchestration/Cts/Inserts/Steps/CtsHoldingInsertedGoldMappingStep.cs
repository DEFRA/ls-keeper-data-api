using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(3)]
public class CtsHoldingInsertedGoldMappingStep(ILogger<CtsHoldingInsertedGoldMappingStep> logger)
    : ImportStepBase<CtsHoldingInsertedContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingInsertedContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}