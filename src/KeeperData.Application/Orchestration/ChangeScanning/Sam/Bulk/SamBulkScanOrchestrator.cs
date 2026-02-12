using KeeperData.Core.Telemetry;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;

public class SamBulkScanOrchestrator(IEnumerable<IScanStep<SamBulkScanContext>> steps, IApplicationMetrics metrics)
    : ScanOrchestrator<SamBulkScanContext>(steps, metrics)
{
}