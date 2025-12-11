namespace KeeperData.Application.Orchestration.Updates.Cts.Holdings;

public class CtsUpdateHoldingOrchestrator(IEnumerable<IUpdateStep<CtsUpdateHoldingContext>> steps)
    : UpdateOrchestrator<CtsUpdateHoldingContext>(steps)
{
}