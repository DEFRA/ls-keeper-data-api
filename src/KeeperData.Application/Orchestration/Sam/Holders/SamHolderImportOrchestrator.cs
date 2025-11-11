namespace KeeperData.Application.Orchestration.Sam.Holders;

public class SamHolderImportOrchestrator(IEnumerable<IImportStep<SamHolderImportContext>> steps)
    : ImportOrchestrator<SamHolderImportContext>(steps)
{
}