using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class CtsBulkScanJob(
    ICtsBulkScanTask ctsBulkScanTask,
    ILogger<CtsBulkScanJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("CtsBulkScanJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await ctsBulkScanTask.RunAsync(context.CancellationToken);

            logger.LogInformation("CtsBulkScanJob completed at {endTime}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CtsBulkScanJob failed.");
            throw;
        }
    }
}