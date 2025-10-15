using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Updates.Steps;

[StepOrder(2)]
public class CtsHoldingUpdateSilverMappingStep(ILogger<CtsHoldingUpdateSilverMappingStep> logger)
    : ImportStepBase<CtsHoldingUpdateContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingUpdateContext context, CancellationToken cancellationToken)
    {
        if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeUpdate })
            return;

        // TODO - Add implementation

        await Task.CompletedTask;
    }
}