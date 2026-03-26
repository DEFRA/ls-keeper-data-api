using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class CtsScanJob(
    ICtsScanTask ctsScanTask,
    ILogger<CtsScanJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("CtsScanJob started at {StartTime}", DateTime.UtcNow);

        try
        {
            await ctsScanTask.RunAsync(context.CancellationToken);

            logger.LogInformation("CtsScanJob completed at {EndTime}", DateTime.UtcNow);
        }
        catch
        {
            throw;
        }
    }
}
