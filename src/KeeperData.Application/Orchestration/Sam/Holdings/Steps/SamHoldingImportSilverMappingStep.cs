using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Steps;

[StepOrder(2)]
public class SamHoldingImportSilverMappingStep(ILogger<SamHoldingImportSilverMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        //if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeInsert })
        //    return;

        //context.SilverHolding = SamHoldingMapper.ToSilver(context.RawHolding);

        //context.SilverParties = [
        //    .. SamHolderMapper.ToSilver(context.RawHolders),
        //    .. SamPartyMapper.ToSilver(context.RawParties, context.RawHerds)
        //];

        //context.SilverPartyRoles = []; // Map From SilverParties

        await Task.CompletedTask;
    }
}