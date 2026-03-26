using KeeperData.Api.Worker.Tasks;
using Quartz;

namespace KeeperData.Api.Tests.Component.Scheduling.FakeJobs;

[DisallowConcurrentExecution]
public class FakeCtsScanJob(ICtsScanTask task) : IJob
{
    private readonly ICtsScanTask _task = task;

    public async Task Execute(IJobExecutionContext context)
    {
        await _task.RunAsync(context.CancellationToken);
    }
}
