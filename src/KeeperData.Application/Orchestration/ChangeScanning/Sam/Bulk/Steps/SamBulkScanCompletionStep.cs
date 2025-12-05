using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk.Steps;

[StepOrder(999)]
public class SamBulkScanCompletionStep(
    IBatchCompletionNotificationService batchCompletionService,
    ILogger<SamBulkScanCompletionStep> logger) : ScanStepBase<SamBulkScanContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamBulkScanContext context, CancellationToken cancellationToken)
    {
        await batchCompletionService.NotifyBatchCompletionAsync(context, cancellationToken);
    }
}