using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(3)]
public class CtsHoldingInsertGoldMappingStep(ILogger<CtsHoldingInsertGoldMappingStep> logger)
    : ImportStepBase<CtsHoldingInsertContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingInsertContext context, CancellationToken cancellationToken)
    {
        if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeInsert })
            return;

        await Task.CompletedTask;
    }
}