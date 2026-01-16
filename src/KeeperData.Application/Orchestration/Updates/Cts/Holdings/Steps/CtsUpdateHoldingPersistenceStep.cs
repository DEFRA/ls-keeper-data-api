using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Extensions;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;

[StepOrder(3)]
public class CtsUpdateHoldingPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsUpdateHoldingPersistenceStep> logger)
    : UpdateStepBase<CtsUpdateHoldingContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;

    protected override async Task ExecuteCoreAsync(CtsUpdateHoldingContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHolding != null)
        {
            await UpsertSilverHoldingAsync(context.SilverHolding, cancellationToken);
        }

        await UpsertSilverPartiesAndDeleteOrphansAsync(context.CphTrimmed, context.SilverParties, cancellationToken);
    }

    private async Task UpsertSilverHoldingAsync(CtsHoldingDocument incomingHolding, CancellationToken cancellationToken)
    {
        var existingHolding = await _silverHoldingRepository.FindOneAsync(
            x => x.CountyParishHoldingNumber == incomingHolding.CountyParishHoldingNumber,
            cancellationToken);

        if (existingHolding is null)
        {
            incomingHolding.Id = Guid.NewGuid().ToString();
            await _silverHoldingRepository.AddAsync(incomingHolding, cancellationToken);
        }
        else
        {
            incomingHolding.Id = existingHolding.Id;
            await _silverHoldingRepository.UpdateAsync(incomingHolding, cancellationToken);
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
            await _silverPartyRepository.AddManyAsync(newItems, cancellationToken);

        if (updateItems.Count > 0)
            await _silverPartyRepository.BulkUpdateWithCustomFilterAsync(updateItems, cancellationToken);

        var orphanedParties = existingParties
            .Where(e => !incomingKeys.Contains($"{e.PartyId}::{e.CountyParishHoldingNumber}"))
            .ToList();

        if (orphanedParties.Count > 0)
        {
            var deleteFilter = Builders<CtsPartyDocument>.Filter.In(
                x => x.Id,
                orphanedParties.Select(d => d.Id)
            );

            await _silverPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task<List<CtsPartyDocument>> GetExistingSilverPartiesAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        return await _silverPartyRepository.FindAsync(
            x => x.CountyParishHoldingNumber == holdingIdentifier,
            cancellationToken) ?? [];
    }
}