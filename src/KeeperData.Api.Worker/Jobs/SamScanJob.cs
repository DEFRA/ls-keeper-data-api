using KeeperData.Api.Worker.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace KeeperData.Api.Worker.Jobs;

[DisallowConcurrentExecution]
public class SamScanJob(
    ISamScanTask samScanTask,
    ILogger<SamScanJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("SamScanJob started at {StartTime}", DateTime.UtcNow);

        try
        {
            await samScanTask.RunAsync(context.CancellationToken);

            logger.LogInformation("SamScanJob completed at {EndTime}", DateTime.UtcNow);
        }
        catch
        {
            throw;
        }
    }
}
