namespace KeeperData.Application.Orchestration.Cts.Inserts;

public class CtsHoldingInsertOrchestrator(IEnumerable<IImportStep<CtsHoldingInsertContext>> steps)
    : ImportOrchestrator<CtsHoldingInsertContext>(steps)
{
}