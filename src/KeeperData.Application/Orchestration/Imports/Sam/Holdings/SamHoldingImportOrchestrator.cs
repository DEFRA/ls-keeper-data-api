namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings;

public class SamHoldingImportOrchestrator(IEnumerable<IImportStep<SamHoldingImportContext>> steps)
    : ImportOrchestrator<SamHoldingImportContext>(steps)
{
}