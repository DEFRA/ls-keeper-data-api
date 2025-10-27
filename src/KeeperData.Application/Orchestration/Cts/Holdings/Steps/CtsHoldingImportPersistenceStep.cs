using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Cts.Holdings.Steps;

[StepOrder(4)]
public class CtsHoldingImportPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    IGenericRepository<PartyRoleRelationshipDocument> silverPartyRoleRelationshipRepository,
    ILogger<CtsHoldingImportPersistenceStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<PartyRoleRelationshipDocument> _silverPartyRoleRelationshipRepository = silverPartyRoleRelationshipRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHoldings?.Count > 0)
        {
            var primaryHolding = context.SilverHoldings[0];
            await UpsertPrimaryHoldingAsync(primaryHolding, cancellationToken);
        }

        if (context.SilverParties?.Count > 0)
        {
            await UpsertPartiesAndDeleteOrphansAsync(context.SilverParties, cancellationToken);
        }

        if (context.SilverPartyRoles?.Count > 0)
        {
            await ReplacePartyRolesAsync(context.SilverPartyRoles, cancellationToken);
        }
    }

    private async Task UpsertPrimaryHoldingAsync(CtsHoldingDocument incomingHolding, CancellationToken cancellationToken)
    {
        var existingHolding = await _silverHoldingRepository.FindOneAsync(x => x.CountyParishHoldingNumber == incomingHolding.CountyParishHoldingNumber,
            cancellationToken);

        incomingHolding.Id = existingHolding?.Id ?? Guid.NewGuid().ToString();

        var holdingUpsert = (
            Filter: Builders<CtsHoldingDocument>.Filter.Eq(
                x => x.CountyParishHoldingNumber, incomingHolding.CountyParishHoldingNumber),
            Entity: incomingHolding);

        await _silverHoldingRepository.BulkUpsertWithCustomFilterAsync(
            [holdingUpsert], cancellationToken);
    }

    private async Task UpsertPartiesAndDeleteOrphansAsync(List<CtsPartyDocument> incomingParties, CancellationToken cancellationToken)
    {
        var incomingKeys = incomingParties
            .Select(p => $"{p.PartyId}::{p.CountyParishHoldingNumber}")
            .ToHashSet();

        var existingParties = await GetExistingPartiesAsync(incomingParties, cancellationToken);

        var partyUpserts = incomingParties
            .Select(p =>
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

        var orphanedParties = existingParties
            .Where(e => !incomingKeys.Contains($"{e.PartyId}::{e.CountyParishHoldingNumber}"))
            .ToList();

        if (orphanedParties.Count > 0)
        {
            var deleteFilter = Builders<CtsPartyDocument>.Filter.In(x => x.Id, orphanedParties.Select(d => d.Id));
            await _silverPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task<List<CtsPartyDocument>> GetExistingPartiesAsync(List<CtsPartyDocument> incomingParties, CancellationToken cancellationToken)
    {
        var holdingIds = incomingParties
            .Select(p => p.CountyParishHoldingNumber)
            .Distinct()
            .ToList();

        return await _silverPartyRepository.FindAsync(
            x => holdingIds.Contains(x.CountyParishHoldingNumber),
            cancellationToken);
    }

    private async Task ReplacePartyRolesAsync(List<PartyRoleRelationshipDocument> incomingPartyRoles, CancellationToken cancellationToken)
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