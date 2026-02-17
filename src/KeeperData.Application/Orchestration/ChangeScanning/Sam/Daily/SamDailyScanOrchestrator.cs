using KeeperData.Core.Telemetry;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;

public class SamDailyScanOrchestrator(IEnumerable<IScanStep<SamDailyScanContext>> steps, IApplicationMetrics metrics)
    : ScanOrchestrator<SamDailyScanContext>(steps, metrics)
{
}