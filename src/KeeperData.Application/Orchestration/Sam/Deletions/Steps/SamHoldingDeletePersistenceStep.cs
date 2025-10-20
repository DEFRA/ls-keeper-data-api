using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Sam.Deletions.Steps;

[StepOrder(1)]
public class SamHoldingDeletePersistenceStep(
    IGenericRepository<SamHoldingDocument> silverHoldingRepository,
    IGenericRepository<SiteDocument> goldSiteRepository,
    ILogger<SamHoldingDeletePersistenceStep> logger)
    : ImportStepBase<SamHoldingDeleteContext>(logger)
{
    private readonly IGenericRepository<SamHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<SiteDocument> _goldSiteRepository = goldSiteRepository;

    protected override async Task ExecuteCoreAsync(SamHoldingDeleteContext context, CancellationToken cancellationToken)
    {
        // Find silver holding > mark as deleted
        var silverHoldingResults = await _silverHoldingRepository
            .FindAsync(x => x.CountyParishHoldingNumber.Equals(context.Cph, StringComparison.InvariantCultureIgnoreCase),
            cancellationToken);

        // Find gold holding > mark as deleted
        var identifierFilter = Builders<SiteIdentifierDocument>.Filter.And(
            Builders<SiteIdentifierDocument>.Filter.Eq(x => x.Type, HoldingIdentifierType.HoldingNumber.ToString()),
            Builders<SiteIdentifierDocument>.Filter.Eq(x => x.Identifier, context.Cph));

        var goldHoldingResults = await _goldSiteRepository.FindAsync(
            s => s.Identifiers,
            identifierFilter,
            cancellationToken);

        context.SilverHoldings = [];
        context.GoldSites = [];

        foreach (var silver in silverHoldingResults ?? [])
        {
            silver.Deleted = true;
            context.SilverHoldings.Add(silver);
        }

        foreach (var gold in goldHoldingResults ?? [])
        {
            gold.Deleted = true;
            context.GoldSites.Add(gold);
        }

        if (context.SilverHoldings.Count != 0)
            await _silverHoldingRepository.BulkUpsertAsync(context.SilverHoldings, cancellationToken);

        if (context.GoldSites.Count != 0)
            await _goldSiteRepository.BulkUpsertAsync(context.GoldSites, cancellationToken);

        await Task.CompletedTask;
    }
}