namespace KeeperData.Application.Orchestration.Sam.Updates;

public class SamHoldingUpdateOrchestrator(IEnumerable<IImportStep<SamHoldingUpdateContext>> steps)
    : ImportOrchestrator<SamHoldingUpdateContext>(steps)
{
}
