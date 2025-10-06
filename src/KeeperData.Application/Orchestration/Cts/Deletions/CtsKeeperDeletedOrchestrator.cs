namespace KeeperData.Application.Orchestration.Cts.Deletions;

public class CtsKeeperDeletedOrchestrator(IEnumerable<IImportStep<CtsKeeperDeleteContext>> steps)
    : ImportOrchestrator<CtsKeeperDeleteContext>(steps)
{
}
