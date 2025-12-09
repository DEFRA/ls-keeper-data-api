using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;

[StepOrder(5)]
public class SamPartyDailyScanCompletionStep(
    IBatchCompletionNotificationService batchCompletionService,
    ILogger<SamPartyDailyScanCompletionStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        await batchCompletionService.NotifyBatchCompletionAsync(context, cancellationToken);
    }
}