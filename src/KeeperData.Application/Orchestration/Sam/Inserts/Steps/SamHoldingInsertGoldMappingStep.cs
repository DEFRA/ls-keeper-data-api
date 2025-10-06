using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Inserts.Steps;

[StepOrder(3)]
public class SamHoldingInsertGoldMappingStep(ILogger<SamHoldingInsertGoldMappingStep> logger)
    : ImportStepBase<SamHoldingInsertContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingInsertContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}