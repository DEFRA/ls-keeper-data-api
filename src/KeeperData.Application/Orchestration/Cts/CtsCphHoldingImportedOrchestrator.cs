namespace KeeperData.Application.Orchestration.Cts;

public class CtsCphHoldingImportedOrchestrator(IEnumerable<IImportStep<CtsHoldingImportContext>> steps)
    : ImportOrchestrator<CtsHoldingImportContext>(steps)
{
}