using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class ScanCTSBulkFilesJob(
    ITaskScanCTSBulkFiles taskScanBulkFiles,
    ILogger<ScanCTSBulkFilesJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("ScanCTSBulkFilesJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await taskScanBulkFiles.RunAsync(context.CancellationToken);

            logger.LogInformation("ScanCTSBulkFilesJob completed at {endTime}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ScanCTSBulkFilesJob failed.");
            throw;
        }
    }
}