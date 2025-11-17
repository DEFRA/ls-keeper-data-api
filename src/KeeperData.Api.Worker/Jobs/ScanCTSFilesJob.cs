using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class ScanCTSFilesJob(
    ITaskScanCTSFiles taskScanCTSFiles,
    ILogger<ScanCTSFilesJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("ScanCTSFilesJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await taskScanCTSFiles.RunAsync(context.CancellationToken);

            logger.LogInformation("ScanCTSFilesJob completed at {endTime}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ScanCTSFilesJob failed.");
            throw;
        }
    }
}