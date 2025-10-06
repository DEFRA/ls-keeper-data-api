namespace KeeperData.Application.Orchestration.Cts.Deletions;

public class CtsHoldingDeletedOrchestrator(IEnumerable<IImportStep<CtsHoldingDeleteContext>> steps)
    : ImportOrchestrator<CtsHoldingDeleteContext>(steps)
{
}
