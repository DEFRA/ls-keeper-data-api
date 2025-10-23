using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Steps;

[StepOrder(3)]
public class SamHoldingImportGoldMappingStep(ILogger<SamHoldingImportGoldMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        //if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeInsert })
        //    return;

        // TODO - Add implementation

        await Task.CompletedTask;
    }
}