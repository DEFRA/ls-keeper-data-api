using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class ScanSAMBulkFilesJob(
    ITaskScanSAMBulkFiles taskScanBulkFiles,
    ILogger<ScanSAMBulkFilesJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("ScanSAMBulkFilesJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await taskScanBulkFiles.RunAsync(context.CancellationToken);

            logger.LogInformation("ScanSAMBulkFilesJob completed at {endTime}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ScanSAMBulkFilesJob failed.");
            throw;
        }
    }
}