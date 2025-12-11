using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(5)]
public class SamHoldingImportPersistenceStep(
    IGenericRepository<SamHoldingDocument> silverHoldingRepository,
    IGenericRepository<SamPartyDocument> silverPartyRepository,
    IGenericRepository<SamHerdDocument> silverHerdRepository,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    IGoldSitePartyRoleRelationshipRepository goldSitePartyRoleRelationshipRepository,
    ILogger<SamHoldingImportPersistenceStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    private readonly IGenericRepository<SamHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<SamPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<SamHerdDocument> _silverHerdRepository = silverHerdRepository;

    private readonly IGenericRepository<SiteDocument> _goldSiteRepository = goldSiteRepository;
    private readonly IGenericRepository<PartyDocument> _goldPartyRepository = goldPartyRepository;
    private readonly IGoldSitePartyRoleRelationshipRepository _goldSitePartyRoleRelationshipRepository = goldSitePartyRoleRelationshipRepository;

    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        await UpsertSilverHoldingsAndDeleteOrphansAsync(context.Cph, context.SilverHoldings, cancellationToken);

        await UpsertSilverPartiesAsync(context.SilverParties, cancellationToken);

        await UpsertSilverHerdsAndDeleteOrphansAsync(context.Cph, context.SilverHerds, cancellationToken);

        if (context.GoldSite != null)
        {
            await UpsertGoldSiteAsync(context.GoldSite, cancellationToken);
        }

        await UpsertGoldPartiesAsync(context.GoldParties, cancellationToken);

        await UpsertGoldPartyRolesAndDeleteOrphansAsync(
            context.Cph,
            context.GoldSitePartyRoles,
            cancellationToken);
    }

    private async Task UpsertSilverHoldingsAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<SamHoldingDocument> incomingHoldings,
        CancellationToken cancellationToken)
    {
        incomingHoldings ??= [];

        var incomingKeys = incomingHoldings
            .Select(p => $"{p.CountyParishHoldingNumber}::{p.LocationName}::{p.SecondaryCph}::{p.SpeciesTypeCode}")
            .ToHashSet();

        var existingHoldings = await GetExistingSilverHoldingsAsync(holdingIdentifier, cancellationToken);

        if (incomingHoldings.Count > 0)
        {
            var upserts = incomingHoldings.Select(p =>
            {
                var existing = existingHoldings.FirstOrDefault(e =>
                    e.CountyParishHoldingNumber == p.CountyParishHoldingNumber
                    && e.LocationName == p.LocationName
                    && e.SpeciesTypeCode == p.SpeciesTypeCode
                    && e.SecondaryCph == p.SecondaryCph);

                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                return (
                    Filter: Builders<SamHoldingDocument>.Filter.And(
                        Builders<SamHoldingDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, p.CountyParishHoldingNumber),
                        Builders<SamHoldingDocument>.Filter.Eq(x => x.LocationName, p.LocationName),
                        Builders<SamHoldingDocument>.Filter.Eq(x => x.SpeciesTypeCode, p.SpeciesTypeCode),
                        Builders<SamHoldingDocument>.Filter.Eq(x => x.SecondaryCph, p.SecondaryCph)
                    ),
                    Entity: p
                );
            });

            await _silverHoldingRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
        }

        var orphanedHoldings = existingHoldings?
            .Where(e => !incomingKeys.Contains($"{e.CountyParishHoldingNumber}::{e.LocationName}::{e.SecondaryCph}::{e.SpeciesTypeCode}"))
            .ToList() ?? [];

        if (orphanedHoldings?.Count > 0)
        {
            var deleteFilter = Builders<SamHoldingDocument>.Filter.In(
                x => x.Id,
                orphanedHoldings.Select(d => d.Id)
            );

            await _silverHoldingRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task UpsertSilverPartiesAsync(
        List<SamPartyDocument> incomingParties,
        CancellationToken cancellationToken)
    {
        incomingParties ??= [];

        var incomingPartyIds = incomingParties
            .Select(p => p.PartyId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet();

        var upserts = new List<(FilterDefinition<SamPartyDocument> Filter, SamPartyDocument Entity)>();

        foreach (var incoming in incomingParties)
        {
            if (string.IsNullOrWhiteSpace(incoming.PartyId))
                continue;

            var existing = await _silverPartyRepository.FindOneAsync(
                x => x.PartyId == incoming.PartyId,
                cancellationToken);

            incoming.Id = existing?.Id ?? Guid.NewGuid().ToString();

            var filter = Builders<SamPartyDocument>.Filter.Eq(x => x.PartyId, incoming.PartyId);
            upserts.Add((filter, incoming));
        }

        if (upserts.Count > 0)
        {
            await _silverPartyRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
        }
    }

    private async Task UpsertSilverHerdsAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<SamHerdDocument> incomingHerds,
        CancellationToken cancellationToken)
    {
        incomingHerds ??= [];

        var incomingKeys = incomingHerds
            .Select(p => $"{p.CountyParishHoldingHerd}::{p.ProductionUsageCode}::{p.Herdmark}")
            .ToHashSet();

        var existingHerds = await GetExistingSilverHerdsAsync(holdingIdentifier, cancellationToken);

        if (incomingHerds.Count > 0)
        {
            var upserts = incomingHerds.Select(p =>
            {
                var existing = existingHerds.FirstOrDefault(e =>
                    e.CountyParishHoldingHerd == p.CountyParishHoldingHerd
                    && e.ProductionUsageCode == p.ProductionUsageCode
                    && e.Herdmark == p.Herdmark);

                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                return (
                    Filter: Builders<SamHerdDocument>.Filter.And(
                        Builders<SamHerdDocument>.Filter.Eq(x => x.CountyParishHoldingHerd, p.CountyParishHoldingHerd),
                        Builders<SamHerdDocument>.Filter.Eq(x => x.ProductionUsageCode, p.ProductionUsageCode),
                        Builders<SamHerdDocument>.Filter.Eq(x => x.Herdmark, p.Herdmark)
                    ),
                    Entity: p
                );
            });

            await _silverHerdRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
        }

        var orphanedHerds = existingHerds?
            .Where(e => !incomingKeys.Contains($"{e.CountyParishHoldingHerd}::{e.ProductionUsageCode}::{e.Herdmark}"))
            .ToList() ?? [];

        if (orphanedHerds?.Count > 0)
        {
            var deleteFilter = Builders<SamHerdDocument>.Filter.In(
                x => x.Id,
                orphanedHerds.Select(d => d.Id)
            );

            await _silverHerdRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task UpsertGoldSiteAsync(
        SiteDocument incomingSite,
        CancellationToken cancellationToken)
    {
        var holdingIdentifierType = incomingSite.Identifiers.FirstOrDefault()?.Type
            ?? HoldingIdentifierType.CphNumber.ToString();

        var holdingIdentifier = incomingSite.Identifiers.FirstOrDefault()?.Identifier
            ?? string.Empty;

        var filter = Builders<SiteDocument>.Filter.ElemMatch(
            x => x.Identifiers,
            i => i.Identifier == holdingIdentifier && i.Type == holdingIdentifierType);

        // Done in mapper now using domain objects
        // var existingHolding = await _goldSiteRepository.FindOneByFilterAsync(filter, cancellationToken);
        // incomingSite.Id = existingHolding?.Id ?? Guid.NewGuid().ToString();

        var siteUpsert = (
            Filter: filter,
            Entity: incomingSite);

        await _goldSiteRepository.BulkUpsertWithCustomFilterAsync(
            [siteUpsert], cancellationToken);
    }

    private async Task UpsertGoldPartiesAsync(
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

            // Done in mapper now using domain objects
            // var existing = await _goldPartyRepository.FindOneAsync(
            //    x => x.CustomerNumber == incoming.CustomerNumber,
            //    cancellationToken);
            // incoming.Id = existing?.Id ?? Guid.NewGuid().ToString();

            var filter = Builders<PartyDocument>.Filter.Eq(x => x.CustomerNumber, incoming.CustomerNumber);
            upserts.Add((filter, incoming));
        }

        if (upserts.Count > 0)
        {
            await _goldPartyRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
        }
    }

    private async Task UpsertGoldPartyRolesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<Core.Documents.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        incomingSitePartyRoles ??= [];

        var incomingKeys = incomingSitePartyRoles
            .Select(p => $"{p.HoldingIdentifier}::{p.PartyId}::{p.RoleTypeId}::{p.SpeciesTypeId}")
            .ToHashSet();

        var existingSitePartyRoles = await GetExistingGoldSitePartyRoleRelationshipsAsync(
            holdingIdentifier,
            cancellationToken);

        if (incomingSitePartyRoles.Count > 0)
        {
            var upserts = incomingSitePartyRoles.Select(p =>
            {
                var existing = existingSitePartyRoles.FirstOrDefault(e =>
                    e.PartyId == p.PartyId &&
                    e.HoldingIdentifier == p.HoldingIdentifier &&
                    e.RoleTypeId == p.RoleTypeId &&
                    e.SpeciesTypeId == p.SpeciesTypeId);

                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                return (
                    Filter: Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.And(
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, p.HoldingIdentifier),
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, p.RoleTypeId),
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.SpeciesTypeId, p.SpeciesTypeId)
                    ),
                    Entity: p
                );
            });

            await _goldSitePartyRoleRelationshipRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
        }

        var orphanedSitePartyRoles = existingSitePartyRoles?.Where(e =>
            !incomingKeys.Contains($"{e.HoldingIdentifier}::{e.PartyId}::{e.RoleTypeId}::{e.SpeciesTypeId}"))
        .ToList() ?? [];

        if (orphanedSitePartyRoles.Count > 0)
        {
            var deleteFilter = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.In(
                x => x.Id,
                orphanedSitePartyRoles.Select(d => d.Id));

            await _goldSitePartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task<List<SamHoldingDocument>> GetExistingSilverHoldingsAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        return await _silverHoldingRepository.FindAsync(
            x => x.CountyParishHoldingNumber == holdingIdentifier,
            cancellationToken) ?? [];
    }

    private async Task<List<SamHerdDocument>> GetExistingSilverHerdsAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        return await _silverHerdRepository.FindAsync(
            x => x.CountyParishHoldingNumber == holdingIdentifier,
            cancellationToken) ?? [];
    }

    private async Task<List<Core.Documents.SitePartyRoleRelationshipDocument>> GetExistingGoldSitePartyRoleRelationshipsAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        return await _goldSitePartyRoleRelationshipRepository.FindAsync(
            x => x.HoldingIdentifier == holdingIdentifier,
            cancellationToken) ?? [];
    }
}