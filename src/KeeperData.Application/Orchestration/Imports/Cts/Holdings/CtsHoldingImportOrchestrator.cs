namespace KeeperData.Application.Orchestration.Imports.Cts.Holdings;

public class CtsHoldingImportOrchestrator(IEnumerable<IImportStep<CtsHoldingImportContext>> steps)
    : ImportOrchestrator<CtsHoldingImportContext>(steps)
{
}