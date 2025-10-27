using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Steps;

[StepOrder(4)]
public class SamHoldingImportPersistenceStep(
    IGenericRepository<SamHoldingDocument> silverHoldingRepository,
    IGenericRepository<SamPartyDocument> silverPartyRepository,
    IGenericRepository<PartyRoleRelationshipDocument> silverPartyRoleRelationshipRepository,
    IGenericRepository<SamHerdDocument> silverHerdRepository,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamHoldingImportPersistenceStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    private readonly IGenericRepository<SamHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<SamPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<PartyRoleRelationshipDocument> _silverPartyRoleRelationshipRepository = silverPartyRoleRelationshipRepository;
    private readonly IGenericRepository<SamHerdDocument> _silverHerdRepository = silverHerdRepository;

    private readonly IGenericRepository<SiteDocument> _goldSiteRepository = goldSiteRepository;
    private readonly IGenericRepository<PartyDocument> _goldPartyRepository = goldPartyRepository;

    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHoldings?.Count > 0)
        {
            var primaryHolding = context.SilverHoldings[0];
            await UpsertPrimaryHoldingAsync(primaryHolding, cancellationToken);
        }

        await UpsertPartiesAndDeleteOrphansAsync(context.Cph, context.SilverParties, cancellationToken);

        await ReplacePartyRolesAsync(context.Cph, context.SilverPartyRoles, cancellationToken);

        await ReplaceHerdsAsync(context.Cph, context.SilverHerds, cancellationToken);
    }

    private async Task UpsertPrimaryHoldingAsync(
        SamHoldingDocument incomingHolding,
        CancellationToken cancellationToken)
    {
        var existingHolding = await _silverHoldingRepository.FindOneAsync(
            x => x.CountyParishHoldingNumber == incomingHolding.CountyParishHoldingNumber,
            cancellationToken);

        incomingHolding.Id = existingHolding?.Id ?? Guid.NewGuid().ToString();

        var holdingUpsert = (
            Filter: Builders<SamHoldingDocument>.Filter.Eq(
                x => x.CountyParishHoldingNumber, incomingHolding.CountyParishHoldingNumber),
            Entity: incomingHolding);

        await _silverHoldingRepository.BulkUpsertWithCustomFilterAsync(
            [holdingUpsert], cancellationToken);
    }

    private async Task UpsertPartiesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<SamPartyDocument> incomingParties,
        CancellationToken cancellationToken)
    {
        incomingParties ??= [];

        var incomingKeys = incomingParties
            .Select(p => $"{p.PartyId}::{p.CountyParishHoldingNumber}")
            .ToHashSet();

        var existingParties = await GetExistingPartiesAsync(holdingIdentifier, cancellationToken);

        if (incomingParties.Count > 0)
        {
            var partyUpserts = incomingParties.Select(p =>
            {
                var existing = existingParties.FirstOrDefault(e =>
                    e.PartyId == p.PartyId &&
                    e.CountyParishHoldingNumber == p.CountyParishHoldingNumber);

                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                return (
                    Filter: Builders<SamPartyDocument>.Filter.And(
                        Builders<SamPartyDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<SamPartyDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, p.CountyParishHoldingNumber)
                    ),
                    Entity: p
                );
            });

            await _silverPartyRepository.BulkUpsertWithCustomFilterAsync(partyUpserts, cancellationToken);
        }

        var orphanedParties = existingParties?
            .Where(e => !incomingKeys.Contains($"{e.PartyId}::{e.CountyParishHoldingNumber}"))
            .ToList() ?? [];

        if (orphanedParties?.Count > 0)
        {
            var deleteFilter = Builders<SamPartyDocument>.Filter.In(
                x => x.Id,
                orphanedParties.Select(d => d.Id)
            );

            await _silverPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task<List<SamPartyDocument>> GetExistingPartiesAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        return await _silverPartyRepository.FindAsync(
            x => x.CountyParishHoldingNumber == holdingIdentifier,
            cancellationToken) ?? [];
    }

    private async Task ReplacePartyRolesAsync(
        string holdingIdentifier,
        List<PartyRoleRelationshipDocument> incomingPartyRoles,
        CancellationToken cancellationToken)
    {
        var sourceAsSam = SourceSystemType.SAM.ToString();

        var deleteFilter = Builders<PartyRoleRelationshipDocument>.Filter.And(
            Builders<PartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier),
            Builders<PartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, sourceAsSam)
        );

        await _silverPartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);

        if (incomingPartyRoles?.Count > 0)
        {
            await _silverPartyRoleRelationshipRepository.AddManyAsync(incomingPartyRoles, cancellationToken);
        }
    }

    private async Task ReplaceHerdsAsync(
        string holdingIdentifier,
        List<SamHerdDocument> incomingHerds,
        CancellationToken cancellationToken)
    {
        var deleteFilter = Builders<SamHerdDocument>.Filter.And(
            Builders<SamHerdDocument>.Filter.Eq(x => x.CountyParishHoldingHerd, holdingIdentifier)
        );

        await _silverHerdRepository.DeleteManyAsync(deleteFilter, cancellationToken);

        if (incomingHerds?.Count > 0)
        {
            await _silverHerdRepository.AddManyAsync(incomingHerds, cancellationToken);
        }
    }
}