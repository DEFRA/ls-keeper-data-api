using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
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

        incomingHolding.Id = existingHolding?.Id ?? Guid.NewGuid().ToString();

        if (existingHolding != null)
        {
            await _silverHoldingRepository.UpdateAsync(incomingHolding, cancellationToken);
        }
        else
        {
            await _silverHoldingRepository.AddAsync(incomingHolding, cancellationToken);
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

        if (incomingParties.Count > 0)
        {
            var partyUpserts = incomingParties.Select(p =>
            {
                var existing = existingParties.FirstOrDefault(e =>
                    e.PartyId == p.PartyId &&
                    e.CountyParishHoldingNumber == p.CountyParishHoldingNumber);

                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                return (
                    Filter: Builders<CtsPartyDocument>.Filter.And(
                        Builders<CtsPartyDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<CtsPartyDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, p.CountyParishHoldingNumber)
                    ),
                    Entity: p
                );
            });

            await _silverPartyRepository.BulkUpsertWithCustomFilterAsync(partyUpserts, cancellationToken);
        }

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