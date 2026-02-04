using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Extensions;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Cts.Holdings.Steps;

[StepOrder(4)]
public class CtsHoldingImportPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsHoldingImportPersistenceStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHoldings?.Count > 0)
        {
            var primaryHolding = context.SilverHoldings[0];
            await UpsertSilverHoldingAsync(primaryHolding, cancellationToken);
        }

        await UpsertSilverPartiesAndDeleteOrphansAsync(context.CphTrimmed, context.SilverParties, cancellationToken);
    }

    private async Task UpsertSilverHoldingAsync(CtsHoldingDocument incomingHolding, CancellationToken cancellationToken)
    {
        var existingHolding = await silverHoldingRepository.FindOneAsync(
            x => x.CountyParishHoldingNumber == incomingHolding.CountyParishHoldingNumber,
            cancellationToken);

        if (existingHolding is null)
        {
            incomingHolding.Id = Guid.NewGuid().ToString();
            await silverHoldingRepository.AddAsync(incomingHolding, cancellationToken);
        }
        else
        {
            incomingHolding.Id = existingHolding.Id;
            await silverHoldingRepository.UpdateAsync(incomingHolding, cancellationToken);
        }
    }

    private async Task UpsertSilverPartiesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<CtsPartyDocument> incomingParties,
        CancellationToken cancellationToken)
    {
        incomingParties ??= [];

        var incomingKeys = incomingParties
            .Select(p => $"{p.PartyId}::{p.CountyParishHoldingNumber}")
            .ToHashSet();

        var existingParties = await GetExistingSilverPartiesAsync(holdingIdentifier, cancellationToken);

        var newItems = new List<CtsPartyDocument>();
        var updateItems = new List<(FilterDefinition<CtsPartyDocument> Filter, UpdateDefinition<CtsPartyDocument> Update)>();

        foreach (var incoming in incomingParties)
        {
            var existing = existingParties.FirstOrDefault(e =>
                e.PartyId == incoming.PartyId &&
                e.CountyParishHoldingNumber == incoming.CountyParishHoldingNumber);

            if (existing is null)
            {
                incoming.Id = Guid.NewGuid().ToString();
                newItems.Add(incoming);
            }
            else
            {
                incoming.Id = existing.Id;

                var filter = Builders<CtsPartyDocument>.Filter.Eq(x => x.Id, incoming.Id);
                var update = Builders<CtsPartyDocument>.Update.SetAll(incoming);
                updateItems.Add((filter, update));
            }
        }

        if (newItems.Count > 0)
            await silverPartyRepository.AddManyAsync(newItems, cancellationToken);

        if (updateItems.Count > 0)
            await silverPartyRepository.BulkUpdateWithCustomFilterAsync(updateItems, cancellationToken);

        var orphanedParties = existingParties?
            .Where(e => !incomingKeys.Contains($"{e.PartyId}::{e.CountyParishHoldingNumber}"))
            .ToList() ?? [];

        if (orphanedParties.Count > 0)
        {
            var deleteFilter = Builders<CtsPartyDocument>.Filter.In(
                x => x.Id,
                orphanedParties.Select(d => d.Id)
            );

            await silverPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task<List<CtsPartyDocument>> GetExistingSilverPartiesAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        return await silverPartyRepository.FindAsync(
            x => x.CountyParishHoldingNumber == holdingIdentifier,
            cancellationToken) ?? [];
    }
}