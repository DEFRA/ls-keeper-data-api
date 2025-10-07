namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamHoldingDeleteOrchestrator(IEnumerable<IImportStep<SamHoldingDeleteContext>> steps)
    : ImportOrchestrator<SamHoldingDeleteContext>(steps)
{
}