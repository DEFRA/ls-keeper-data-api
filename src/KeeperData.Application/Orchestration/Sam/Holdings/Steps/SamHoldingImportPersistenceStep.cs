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
            await UpsertSilverHoldingAsync(primaryHolding, cancellationToken);
        }

        await UpsertSilverPartiesAndDeleteOrphansAsync(context.Cph, context.SilverParties, cancellationToken);

        await ReplaceSilverPartyRolesAsync(context.Cph, context.SilverPartyRoles, cancellationToken);

        await ReplaceSilverHerdsAsync(context.Cph, context.SilverHerds, cancellationToken);

        if (context.GoldSite != null)
        {
            await UpsertGoldSiteAsync(context.GoldSite, cancellationToken);
        }

        await UpsertGoldPartiesAndDeleteOrphansAsync(context.Cph, context.GoldParties, cancellationToken);
    }

    private async Task UpsertGoldSiteAsync(
        SiteDocument incomingSite,
        CancellationToken cancellationToken)
    {
        var holdingIdentifierType = incomingSite.Identifiers.FirstOrDefault()?.Type
            ?? HoldingIdentifierType.HoldingNumber.ToString();

        var holdingIdentifier = incomingSite.Identifiers.FirstOrDefault()?.Identifier
            ?? string.Empty;

        var filter = Builders<SiteDocument>.Filter.ElemMatch(
            x => x.Identifiers,
            i => i.Identifier == holdingIdentifier && i.Type == holdingIdentifierType);

        var existingHolding = await _goldSiteRepository.FindOneByFilterAsync(filter, cancellationToken);

        incomingSite.Id = existingHolding?.Id ?? Guid.NewGuid().ToString();

        var siteUpsert = (
            Filter: filter,
            Entity: incomingSite);

        await _goldSiteRepository.BulkUpsertWithCustomFilterAsync(
            [siteUpsert], cancellationToken);
    }

    private async Task UpsertGoldPartiesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<PartyDocument> incomingParties,
        CancellationToken cancellationToken)
    {
        incomingParties ??= [];

        var incomingCustomerNumbers = incomingParties
            .Select(p => p.CustomerNumber)
            .Where(cn => !string.IsNullOrWhiteSpace(cn))
            .ToHashSet();

        var upserts = new List<(FilterDefinition<PartyDocument> Filter, PartyDocument Entity)>();

        foreach (var incoming in incomingParties)
        {
            if (string.IsNullOrWhiteSpace(incoming.CustomerNumber))
                continue;

            var existing = await _goldPartyRepository.FindOneAsync(
                x => x.CustomerNumber == incoming.CustomerNumber,
                cancellationToken);

            incoming.Id = existing?.Id ?? Guid.NewGuid().ToString();

            var filter = Builders<PartyDocument>.Filter.Eq(x => x.CustomerNumber, incoming.CustomerNumber);
            upserts.Add((filter, incoming));
        }

        if (upserts.Count > 0)
        {
            await _goldPartyRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
        }

        // TODO - We need to think about orphans using PartyRoleRelationships

        /*var allExisting = await _goldPartyRepository.FindAsync(
            x => x.CountyParishHoldingNumber == holdingIdentifier,
            cancellationToken) ?? [];

        var orphaned = allExisting
            .Where(e => !incomingCustomerNumbers.Contains(e.CustomerNumber))
            .ToList();

        if (orphaned.Count > 0)
        {
            var deleteFilter = Builders<PartyDocument>.Filter.In(x => x.Id, orphaned.Select(o => o.Id));
            await _goldPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
        */
    }

    private async Task UpsertSilverHoldingAsync(
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

    private async Task UpsertSilverPartiesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<SamPartyDocument> incomingParties,
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

    private async Task<List<SamPartyDocument>> GetExistingSilverPartiesAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        return await _silverPartyRepository.FindAsync(
            x => x.CountyParishHoldingNumber == holdingIdentifier,
            cancellationToken) ?? [];
    }

    private async Task ReplaceSilverPartyRolesAsync(
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

    private async Task ReplaceSilverHerdsAsync(
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