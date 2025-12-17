using Quartz;

namespace KeeperData.Api.Tests.Component.Scheduling.Fakes;

[DisallowConcurrentExecution]
public class FakeCtsBulkScanJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await Task.Delay(1000);
    }
}
