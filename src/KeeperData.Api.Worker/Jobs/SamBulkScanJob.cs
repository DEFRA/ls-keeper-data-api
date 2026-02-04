using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class SamBulkScanJob(
    ISamBulkScanTask samBulkScanTask,
    ILogger<SamBulkScanJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("SamBulkScanJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await samBulkScanTask.RunAsync(context.CancellationToken);

            logger.LogInformation("SamBulkScanJob completed at {endTime}", DateTime.UtcNow);
        }
        catch
        {
            throw;
        }
    }
}