namespace KeeperData.Application.Orchestration.Cts.Deletions;

public class CtsKeeperDeleteOrchestrator(IEnumerable<IImportStep<CtsKeeperDeleteContext>> steps)
    : ImportOrchestrator<CtsKeeperDeleteContext>(steps)
{
}
