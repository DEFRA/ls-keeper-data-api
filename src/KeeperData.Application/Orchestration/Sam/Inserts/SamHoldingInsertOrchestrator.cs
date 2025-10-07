namespace KeeperData.Application.Orchestration.Sam.Inserts;

public class SamHoldingInsertOrchestrator(IEnumerable<IImportStep<SamHoldingInsertContext>> steps)
    : ImportOrchestrator<SamHoldingInsertContext>(steps)
{
}