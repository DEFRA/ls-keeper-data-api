using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Deletions.Steps;

[StepOrder(1)]
public class CtsAgentDeletePersistenceStep(
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsAgentDeletePersistenceStep> logger)
    : ImportStepBase<CtsAgentDeleteContext>(logger)
{
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;

    protected override async Task ExecuteCoreAsync(CtsAgentDeleteContext context, CancellationToken cancellationToken)
    {
        // TODO - Add implementation

        await Task.CompletedTask;
    }
}
