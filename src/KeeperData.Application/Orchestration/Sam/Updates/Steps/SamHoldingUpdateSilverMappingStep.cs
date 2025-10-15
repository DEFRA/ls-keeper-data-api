using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Updates.Steps;

[StepOrder(2)]
public class SamHoldingUpdateSilverMappingStep(ILogger<SamHoldingUpdateSilverMappingStep> logger)
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