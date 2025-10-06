using KeeperData.Application.Orchestration.Cts.Inserts.Mappings;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(2)]
public class CtsHoldingInsertedSilverMappingStep(ILogger<CtsHoldingInsertedSilverMappingStep> logger)
    : ImportStepBase<CtsHoldingInsertedContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingInsertedContext context, CancellationToken cancellationToken)
    {
        if (context.RawHolding == null) return;

        context.SilverHolding = CtsHoldingMapper.ToSilver(context.RawHolding);

        context.SilverParties = [
            .. CtsAgentOrKeeperMapper.ToSilver(context.RawAgents),
            .. CtsAgentOrKeeperMapper.ToSilver(context.RawKeepers)
        ];

        await Task.CompletedTask;
    }
}