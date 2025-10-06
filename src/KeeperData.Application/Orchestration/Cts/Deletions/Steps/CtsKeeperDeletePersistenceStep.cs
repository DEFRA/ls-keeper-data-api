using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Deletions.Steps;

[StepOrder(1)]
public class CtsKeeperDeletePersistenceStep(
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsKeeperDeletePersistenceStep> logger)
    : ImportStepBase<CtsKeeperDeleteContext>(logger)
{
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;

    protected override async Task ExecuteCoreAsync(CtsKeeperDeleteContext context, CancellationToken cancellationToken)
    {
        // TODO - Add implementation

        await Task.CompletedTask;
    }
}
