using KeeperData.Application.Orchestration.Cts.Inserts.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(2)]
public class CtsHoldingInsertSilverMappingStep(ILogger<CtsHoldingInsertSilverMappingStep> logger)
    : ImportStepBase<CtsHoldingInsertContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingInsertContext context, CancellationToken cancellationToken)
    {
        if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeInsert })
            return;

        context.SilverHolding = CtsHoldingMapper.ToSilver(context.RawHolding);

        context.SilverParties = [
            .. CtsAgentOrKeeperMapper.ToSilver(context.RawAgents),
            .. CtsAgentOrKeeperMapper.ToSilver(context.RawKeepers)
        ];

        context.SilverPartyRoles = []; // Map From SilverParties

        await Task.CompletedTask;
    }
}