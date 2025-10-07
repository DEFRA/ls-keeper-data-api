namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamHolderDeleteOrchestrator(IEnumerable<IImportStep<SamHolderDeleteContext>> steps)
    : ImportOrchestrator<SamHolderDeleteContext>(steps)
{
}