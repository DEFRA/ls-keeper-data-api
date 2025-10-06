namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamHoldingDeletedOrchestrator(IEnumerable<IImportStep<SamHoldingDeleteContext>> steps)
    : ImportOrchestrator<SamHoldingDeleteContext>(steps)
{
}
