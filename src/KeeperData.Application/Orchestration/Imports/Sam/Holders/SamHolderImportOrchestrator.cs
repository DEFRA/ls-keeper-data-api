namespace KeeperData.Application.Orchestration.Imports.Sam.Holders;

public class SamHolderImportOrchestrator(IEnumerable<IImportStep<SamHolderImportContext>> steps)
    : ImportOrchestrator<SamHolderImportContext>(steps)
{
}