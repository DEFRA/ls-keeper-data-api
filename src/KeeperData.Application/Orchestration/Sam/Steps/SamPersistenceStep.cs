using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Steps;

public class SamPersistenceStep(
    IGenericRepository<SiteDocument> siteRepository,
    IGenericRepository<PartyDocument> partyRepository,
    ILogger<SamPersistenceStep> logger) 
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    private readonly IGenericRepository<SiteDocument> _siteRepository = siteRepository;
    private readonly IGenericRepository<PartyDocument> _partyRepository = partyRepository;

    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.GoldSite is not null)
            await _siteRepository.BulkUpsertAsync([context.GoldSite], cancellationToken);

        if (context.GoldParties is not null)
            await _partyRepository.BulkUpsertAsync(context.GoldParties, cancellationToken);

        await Task.CompletedTask;
    }
}
