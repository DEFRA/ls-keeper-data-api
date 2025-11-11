namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;

public class CtsBulkScanOrchestrator(IEnumerable<IScanStep<CtsBulkScanContext>> steps)
    : ScanOrchestrator<CtsBulkScanContext>(steps)
{
}
