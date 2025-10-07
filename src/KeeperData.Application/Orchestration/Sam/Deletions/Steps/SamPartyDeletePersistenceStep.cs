using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Deletions.Steps;

[StepOrder(1)]
public class SamPartyDeletePersistenceStep(
    IGenericRepository<SamPartyDocument> silverPartyRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamPartyDeletePersistenceStep> logger)
    : ImportStepBase<SamPartyDeleteContext>(logger)
{
    private readonly IGenericRepository<SamPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<PartyDocument> _goldPartyRepository = goldPartyRepository;

    protected override async Task ExecuteCoreAsync(SamPartyDeleteContext context, CancellationToken cancellationToken)
    {
        // TODO - Add implementation

        await Task.CompletedTask;
    }
}