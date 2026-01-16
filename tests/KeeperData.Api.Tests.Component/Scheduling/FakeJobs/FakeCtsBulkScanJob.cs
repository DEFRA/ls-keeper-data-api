using KeeperData.Api.Worker.Tasks;
using Quartz;

namespace KeeperData.Api.Tests.Component.Scheduling.FakeJobs;

[DisallowConcurrentExecution]
public class FakeCtsBulkScanJob(ICtsBulkScanTask task) : IJob
{
    private readonly ICtsBulkScanTask _task = task;

    public async Task Execute(IJobExecutionContext context)
    {
        await _task.RunAsync(context.CancellationToken);
    }
}