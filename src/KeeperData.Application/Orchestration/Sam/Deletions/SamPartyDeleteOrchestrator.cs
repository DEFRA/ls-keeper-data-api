namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamPartyDeleteOrchestrator(IEnumerable<IImportStep<SamPartyDeleteContext>> steps)
    : ImportOrchestrator<SamPartyDeleteContext>(steps)
{
}