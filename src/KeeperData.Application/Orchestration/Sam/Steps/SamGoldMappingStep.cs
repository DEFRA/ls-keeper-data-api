using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Steps;

public class SamGoldMappingStep(ILogger<SamGoldMappingStep> logger) 
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}
