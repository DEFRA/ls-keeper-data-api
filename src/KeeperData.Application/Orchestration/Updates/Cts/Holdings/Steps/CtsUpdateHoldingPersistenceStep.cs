using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;

[StepOrder(3)]
public class CtsUpdateHoldingPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    IGenericRepository<SitePartyRoleRelationshipDocument> silverPartyRoleRelationshipRepository,
    ILogger<CtsUpdateHoldingPersistenceStep> logger)
    : UpdateStepBase<CtsUpdateHoldingContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<SitePartyRoleRelationshipDocument> _silverPartyRoleRelationshipRepository = silverPartyRoleRelationshipRepository;

    protected override async Task ExecuteCoreAsync(CtsUpdateHoldingContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHolding != null)
        {
            await UpsertSilverHoldingAsync(context.SilverHolding, cancellationToken);
        }

        await UpsertSilverPartiesAndDeleteOrphansAsync(context.CphTrimmed, context.SilverParties, cancellationToken);
        await ReplaceSilverPartyRolesAsync(context.CphTrimmed, context.SilverPartyRoles, cancellationToken);
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

    private async Task ReplaceSilverPartyRolesAsync(
        string holdingIdentifier,
        List<SitePartyRoleRelationshipDocument> incomingPartyRoles,
        CancellationToken cancellationToken)
    {
        var sourceAsCts = SourceSystemType.CTS.ToString();

        var deleteFilter = Builders<SitePartyRoleRelationshipDocument>.Filter.And(
            Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier),
            Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, sourceAsCts)
        );

        await _silverPartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);

        if (incomingPartyRoles.Count > 0)
        {
            await _silverPartyRoleRelationshipRepository.AddManyAsync(incomingPartyRoles, cancellationToken);
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