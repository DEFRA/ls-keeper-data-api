using KeeperData.Application.Orchestration.Cts.Mappings;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Steps;

[StepOrder(2)]
public class CtsSilverMappingStep(ILogger<CtsSilverMappingStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
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