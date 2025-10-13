namespace KeeperData.Application.Orchestration.Cts.Updates;

public class CtsHoldingUpdateOrchestrator(IEnumerable<IImportStep<CtsHoldingUpdateContext>> steps)
    : ImportOrchestrator<CtsHoldingUpdateContext>(steps)
{
}