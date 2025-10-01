using KeeperData.Application.Orchestration.Sam.Mappings;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Steps;

public class SamSilverMappingStep(ILogger<SamSilverMappingStep> logger) 
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.Raw == null) return;

        context.SilverHolding = SamHoldingMapper.ToSilver(context.Raw);

        context.SilverParties = [.. context.Raw.HOLDERS.Select(SamHolderMapper.ToSilver)];

        await Task.CompletedTask;
    }
}
