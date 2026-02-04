using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class CtsDailyScanJob(
    ICtsDailyScanTask ctsDailyScanTask,
    ILogger<CtsDailyScanJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("CtsDailyScanJob started at {startTime}", DateTime.UtcNow);

        try
        {
            await ctsDailyScanTask.RunAsync(context.CancellationToken);

            logger.LogInformation("CtsDailyScanJob completed at {endTime}", DateTime.UtcNow);
        }
        catch
        {
            throw;
        }
    }
}