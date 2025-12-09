using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Completion;

[StepOrder(5)]
public class DailyScanCompletionStep(
    IBatchCompletionNotificationService batchCompletionService,
    ILogger<DailyScanCompletionStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        await batchCompletionService.NotifyBatchCompletionAsync(context, cancellationToken);
    }
}