using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Completion;

[StepOrder(2)]
public class BulkScanCompletionStep(
    IBatchCompletionNotificationService batchCompletionService,
    ILogger<BulkScanCompletionStep> logger) : ScanStepBase<CtsBulkScanContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsBulkScanContext context, CancellationToken cancellationToken)
    {
        await batchCompletionService.NotifyBatchCompletionAsync(context, cancellationToken);
    }
}