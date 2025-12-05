using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk.Steps;

[StepOrder(999)]
public class CtsBulkScanCompletionStep(
    IBatchCompletionNotificationService batchCompletionService,
    ILogger<CtsBulkScanCompletionStep> logger) : ScanStepBase<CtsBulkScanContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsBulkScanContext context, CancellationToken cancellationToken)
    {
        await batchCompletionService.NotifyBatchCompletionAsync(context, cancellationToken);
    }
}