using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(4)]
public class CtsHoldingInsertPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    IGenericRepository<PartyRoleRelationshipDocument> silverPartyRoleRelationshipRepository,
    ILogger<CtsHoldingInsertPersistenceStep> logger)
    : ImportStepBase<CtsHoldingInsertContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<PartyRoleRelationshipDocument> _silverPartyRoleRelationshipRepository = silverPartyRoleRelationshipRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingInsertContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHoldings is not null && context.SilverHoldings.Count != 0)
        {
            var primaryHolding = context.SilverHoldings[0];
            await InsertOrUpdateSilverHoldingAsync(primaryHolding, cancellationToken);
        }

        if (context.SilverParties is not null && context.SilverParties?.Count > 0)
        {
            var incomingKeys = context.SilverParties
                .Select(p => $"{p.PartyId}::{p.CountyParishHoldingNumber}")
                .ToHashSet();

            var existingParties = await GetAllExistingSilverPartiesAsync(context.SilverParties, cancellationToken);

            await InsertOrUpdateSilverPartiesAsync(context.SilverParties, existingParties, cancellationToken);

            await DeleteOrphanedSilverPartiesAsync(incomingKeys, existingParties, cancellationToken);
        }

        if (context.SilverPartyRoles is not null && context.SilverPartyRoles?.Count > 0)
        {
            await DeleteAndRecreateSilverPartyRolesAsync(context.SilverPartyRoles, cancellationToken);
        }
    }

    private async Task InsertOrUpdateSilverHoldingAsync(CtsHoldingDocument incomingHolding, CancellationToken cancellationToken)
    {
        var existingHolding = await _silverHoldingRepository.FindOneAsync(x => x.CountyParishHoldingNumber == incomingHolding.CountyParishHoldingNumber,
            cancellationToken);

        if (existingHolding is not null)
            incomingHolding.Id = existingHolding.Id;

        var holdingUpsert = (
            Filter: Builders<CtsHoldingDocument>.Filter.Eq(
                x => x.CountyParishHoldingNumber, incomingHolding.CountyParishHoldingNumber),
            Entity: incomingHolding);

        await _silverHoldingRepository.BulkUpsertWithCustomFilterAsync(
            [holdingUpsert], cancellationToken);
    }

    private async Task<List<CtsPartyDocument>> GetAllExistingSilverPartiesAsync(List<CtsPartyDocument> silverParties, CancellationToken cancellationToken)
    {
        var holdingIds = silverParties
            .Select(p => p.CountyParishHoldingNumber)
            .Distinct()
            .ToList();

        var existingParties = await _silverPartyRepository.FindAsync(
            x => holdingIds.Contains(x.CountyParishHoldingNumber),
            cancellationToken);

        return existingParties;
    }

    private async Task InsertOrUpdateSilverPartiesAsync(List<CtsPartyDocument> incomingParties, List<CtsPartyDocument> existingParties, CancellationToken cancellationToken)
    {
        var partyUpserts = incomingParties
            .Select(p =>
            {
                var existing = existingParties.FirstOrDefault(e =>
                    e.PartyId == p.PartyId &&
                    e.CountyParishHoldingNumber == p.CountyParishHoldingNumber);

                if (existing is not null)
                    p.Id = existing.Id;

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

    private async Task DeleteOrphanedSilverPartiesAsync(HashSet<string> incomingKeys, List<CtsPartyDocument> existingParties, CancellationToken cancellationToken)
    {
        var toDelete = existingParties
            .Where(e => !incomingKeys.Contains($"{e.PartyId}::{e.CountyParishHoldingNumber}"))
            .ToList();

        if (toDelete.Count != 0)
        {
            var deleteFilter = Builders<CtsPartyDocument>.Filter.In(x => x.Id, toDelete.Select(d => d.Id));
            await _silverPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task DeleteAndRecreateSilverPartyRolesAsync(List<PartyRoleRelationshipDocument> incomingPartyRoles, CancellationToken cancellationToken)
    {
        var holdingIdentifiers = incomingPartyRoles
            .Select(r => r.HoldingIdentifier)
            .Distinct()
            .ToList();

        var deleteFilter = Builders<PartyRoleRelationshipDocument>.Filter.In(x => x.HoldingIdentifier, holdingIdentifiers);

        await _silverPartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);

        await _silverPartyRoleRelationshipRepository.AddManyAsync(incomingPartyRoles, cancellationToken);
    }
}