using KeeperData.Application.Orchestration.Sam.Mappings;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Steps;

[StepOrder(2)]
public class SamSilverMappingStep(ILogger<SamSilverMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.RawHolding == null) return;

        context.SilverHolding = SamHoldingMapper.ToSilver(context.RawHolding);

        context.SilverParties = [
            .. SamHolderMapper.ToSilver(context.RawHolders),
            .. SamPartyMapper.ToSilver(context.RawParties, context.RawHerds)
        ];

        await Task.CompletedTask;
    }
}