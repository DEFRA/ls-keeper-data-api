namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;

public class SamDailyScanOrchestrator(IEnumerable<IScanStep<SamDailyScanContext>> steps)
    : ScanOrchestrator<SamDailyScanContext>(steps)
{
}
