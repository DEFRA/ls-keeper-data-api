namespace KeeperData.Application.Orchestration.Cts.Inserts;

public class CtsHoldingInsertedOrchestrator(IEnumerable<IImportStep<CtsHoldingInsertedContext>> steps)
    : ImportOrchestrator<CtsHoldingInsertedContext>(steps)
{
}