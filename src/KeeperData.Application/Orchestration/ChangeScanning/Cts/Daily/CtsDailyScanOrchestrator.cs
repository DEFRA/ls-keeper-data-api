namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;

public class CtsDailyScanOrchestrator(IEnumerable<IScanStep<CtsDailyScanContext>> steps)
    : ScanOrchestrator<CtsDailyScanContext>(steps)
{
}
