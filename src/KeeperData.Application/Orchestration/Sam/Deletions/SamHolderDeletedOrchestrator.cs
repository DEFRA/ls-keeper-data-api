namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamHolderDeletedOrchestrator(IEnumerable<IImportStep<SamHolderDeleteContext>> steps)
    : ImportOrchestrator<SamHolderDeleteContext>(steps)
{
}
