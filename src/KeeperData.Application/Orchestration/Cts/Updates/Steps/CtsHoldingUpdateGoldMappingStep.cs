using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Updates.Steps;

[StepOrder(3)]
public class CtsHoldingUpdateGoldMappingStep(ILogger<CtsHoldingUpdateGoldMappingStep> logger)
    : ImportStepBase<CtsHoldingUpdateContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingUpdateContext context, CancellationToken cancellationToken)
    {
        if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeUpdate })
            return;

        await Task.CompletedTask;
    }
}