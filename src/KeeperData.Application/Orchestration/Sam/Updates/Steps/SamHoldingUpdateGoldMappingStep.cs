using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Updates.Steps;

[StepOrder(3)]
public class SamHoldingUpdateGoldMappingStep(ILogger<SamHoldingUpdateGoldMappingStep> logger)
    : ImportStepBase<SamHoldingUpdateContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingUpdateContext context, CancellationToken cancellationToken)
    {
        if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeUpdate })
            return;

        // TODO - Add implementation

        await Task.CompletedTask;
    }
}