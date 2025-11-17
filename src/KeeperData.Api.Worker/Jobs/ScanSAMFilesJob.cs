using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class ScanSAMFilesJob(
    ITaskScanSAMFiles taskScanSAMFiles,
    ILogger<ScanSAMFilesJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("ScanSAMFilesJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await taskScanSAMFiles.RunAsync(context.CancellationToken);

            logger.LogInformation("ScanSAMFilesJob completed at {endTime}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ScanSAMFilesJob failed.");
            throw;
        }
    }
}