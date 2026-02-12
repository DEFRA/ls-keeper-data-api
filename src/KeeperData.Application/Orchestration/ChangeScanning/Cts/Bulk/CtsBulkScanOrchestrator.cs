using KeeperData.Core.Telemetry;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;

public class CtsBulkScanOrchestrator(IEnumerable<IScanStep<CtsBulkScanContext>> steps, IApplicationMetrics metrics)
    : ScanOrchestrator<CtsBulkScanContext>(steps, metrics)
{
}