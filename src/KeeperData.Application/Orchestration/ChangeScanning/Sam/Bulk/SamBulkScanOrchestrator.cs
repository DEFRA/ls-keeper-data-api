namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;

public class SamBulkScanOrchestrator(IEnumerable<IScanStep<SamBulkScanContext>> steps)
    : ScanOrchestrator<SamBulkScanContext>(steps)
{
}