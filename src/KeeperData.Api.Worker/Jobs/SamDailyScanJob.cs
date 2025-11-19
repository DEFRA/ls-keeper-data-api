using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class SamDailyScanJob(
    ISamDailyScanTask samDailyScanTask,
    ILogger<SamDailyScanJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("SamDailyScanJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await samDailyScanTask.RunAsync(context.CancellationToken);

            logger.LogInformation("SamDailyScanJob completed at {endTime}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SamDailyScanJob failed.");
            throw;
        }
    }
}