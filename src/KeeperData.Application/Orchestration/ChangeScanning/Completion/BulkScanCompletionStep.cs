using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Completion;

[StepOrder(2)]
public class BulkScanCompletionStep(
    IBatchCompletionNotificationService batchCompletionService,
    ILogger<BulkScanCompletionStep> logger) : ScanStepBase<SamBulkScanContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamBulkScanContext context, CancellationToken cancellationToken)
    {
        await batchCompletionService.NotifyBatchCompletionAsync(context, cancellationToken);
    }
}