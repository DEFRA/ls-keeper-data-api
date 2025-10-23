namespace KeeperData.Application.Orchestration.Cts.Holdings;

public class CtsHoldingImportOrchestrator(IEnumerable<IImportStep<CtsHoldingImportContext>> steps)
    : ImportOrchestrator<CtsHoldingImportContext>(steps)
{
}