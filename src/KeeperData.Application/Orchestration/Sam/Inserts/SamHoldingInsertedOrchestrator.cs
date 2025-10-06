namespace KeeperData.Application.Orchestration.Sam.Inserts;

public class SamHoldingInsertedOrchestrator(IEnumerable<IImportStep<SamHoldingInsertContext>> steps)
    : ImportOrchestrator<SamHoldingInsertContext>(steps)
{
}