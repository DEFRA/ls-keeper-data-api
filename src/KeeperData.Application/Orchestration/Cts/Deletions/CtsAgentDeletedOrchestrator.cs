namespace KeeperData.Application.Orchestration.Cts.Deletions;

public class CtsAgentDeletedOrchestrator(IEnumerable<IImportStep<CtsAgentDeleteContext>> steps)
    : ImportOrchestrator<CtsAgentDeleteContext>(steps)
{
}
