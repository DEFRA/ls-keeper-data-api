namespace KeeperData.Application.Orchestration.Sam;

public class SamCphHoldingImportedOrchestrator(IEnumerable<IImportStep<SamHoldingImportContext>> steps) 
    : ImportOrchestrator<SamHoldingImportContext>(steps)
{
}
