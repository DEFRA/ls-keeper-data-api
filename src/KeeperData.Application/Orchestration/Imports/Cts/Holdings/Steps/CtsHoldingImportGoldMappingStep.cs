using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Cts.Holdings.Steps;

[StepOrder(3)]
public class CtsHoldingImportGoldMappingStep(ILogger<CtsHoldingImportGoldMappingStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    protected override Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}