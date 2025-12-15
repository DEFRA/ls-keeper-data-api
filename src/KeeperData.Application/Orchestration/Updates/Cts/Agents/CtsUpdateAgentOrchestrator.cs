namespace KeeperData.Application.Orchestration.Updates.Cts.Agents;

public class CtsUpdateAgentOrchestrator(IEnumerable<IUpdateStep<CtsUpdateAgentContext>> steps)
    : UpdateOrchestrator<CtsUpdateAgentContext>(steps)
{
}