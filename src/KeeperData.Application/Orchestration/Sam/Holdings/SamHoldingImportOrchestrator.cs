namespace KeeperData.Application.Orchestration.Sam.Holdings;

public class SamHoldingImportOrchestrator(IEnumerable<IImportStep<SamHoldingImportContext>> steps)
    : ImportOrchestrator<SamHoldingImportContext>(steps)
{
}