using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(3)]
public class CtsHoldingInsertGoldMappingStep(ILogger<CtsHoldingInsertGoldMappingStep> logger)
    : ImportStepBase<CtsHoldingInsertContext>(logger)
{
    protected override Task ExecuteCoreAsync(CtsHoldingInsertContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}