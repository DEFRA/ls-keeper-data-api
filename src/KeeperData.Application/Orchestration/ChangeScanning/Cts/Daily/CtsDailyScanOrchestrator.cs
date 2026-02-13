using KeeperData.Core.Telemetry;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;

public class CtsDailyScanOrchestrator(IEnumerable<IScanStep<CtsDailyScanContext>> steps, IApplicationMetrics metrics)
    : ScanOrchestrator<CtsDailyScanContext>(steps, metrics)
{
}