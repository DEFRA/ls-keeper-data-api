namespace KeeperData.Application.Orchestration.Updates.Cts.Keepers;

public class CtsUpdateKeeperOrchestrator(IEnumerable<IUpdateStep<CtsUpdateKeeperContext>> steps)
    : UpdateOrchestrator<CtsUpdateKeeperContext>(steps)
{
}