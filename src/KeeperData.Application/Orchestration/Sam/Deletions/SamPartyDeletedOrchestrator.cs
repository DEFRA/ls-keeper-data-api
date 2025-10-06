namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamPartyDeletedOrchestrator(IEnumerable<IImportStep<SamPartyDeleteContext>> steps)
    : ImportOrchestrator<SamPartyDeleteContext>(steps)
{
}
