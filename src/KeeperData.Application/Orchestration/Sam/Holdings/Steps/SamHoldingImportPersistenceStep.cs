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
    ISilverSitePartyRoleRelationshipRepository silverSitePartyRoleRelationshipRepository,
    IGenericRepository<SamHerdDocument> silverHerdRepository,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument> goldSitePartyRoleRelationshipRepository,
    IGenericRepository<SiteGroupMarkRelationshipDocument> goldSiteGroupMarkRelationshipRepository,
    ILogger<SamHoldingImportPersistenceStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    private readonly IGenericRepository<SamHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<SamPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly ISilverSitePartyRoleRelationshipRepository _silverSitePartyRoleRelationshipRepository = silverSitePartyRoleRelationshipRepository;
    private readonly IGenericRepository<SamHerdDocument> _silverHerdRepository = silverHerdRepository;

    private readonly IGenericRepository<SiteDocument> _goldSiteRepository = goldSiteRepository;
    private readonly IGenericRepository<PartyDocument> _goldPartyRepository = goldPartyRepository;
    private readonly IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument> _goldSitePartyRoleRelationshipRepository = goldSitePartyRoleRelationshipRepository;
    private readonly IGenericRepository<SiteGroupMarkRelationshipDocument> _goldSiteGroupMarkRelationshipRepository = goldSiteGroupMarkRelationshipRepository;

    private const bool IsHolderPartyType = false;

    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        await UpsertSilverHoldingsAndDeleteOrphansAsync(context.Cph, context.SilverHoldings, cancellationToken);

        await UpsertSilverPartiesAndDeleteOrphansAsync(context.Cph, context.SilverParties, cancellationToken);

        await UpsertSilverPartyRolesAndDeleteOrphansAsync(context.Cph, context.SilverPartyRoles, cancellationToken);

        await UpsertSilverHerdsAndDeleteOrphansAsync(context.Cph, context.SilverHerds, cancellationToken);

        if (context.GoldSite != null)
        {
            await UpsertGoldSiteAsync(context.GoldSite, cancellationToken);
        }

        await UpsertGoldPartiesAndDeleteOrphansAsync(context.Cph, context.GoldParties, cancellationToken);

        await ReplaceGoldSitePartyRolesAsync(context.Cph, context.GoldSitePartyRoles, cancellationToken);

        await ReplaceGoldSiteGroupMarksAsync(context.Cph, context.GoldSiteGroupMarks, cancellationToken);
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
            var holdingUpserts = incomingHoldings.Select(p =>
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

            await _silverHoldingRepository.BulkUpsertWithCustomFilterAsync(holdingUpserts, cancellationToken);
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

    private async Task UpsertSilverPartiesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<SamPartyDocument> incomingParties,
        CancellationToken cancellationToken)
    {
        incomingParties ??= [];

        var incomingPartyIds = incomingParties
            .Select(p => p.PartyId)
            .ToHashSet();

        var existingParties = await GetExistingSilverPartiesAsync(holdingIdentifier, cancellationToken);

        if (incomingParties.Count > 0)
        {
            var partyUpserts = incomingParties.Select(p =>
            {
                var existing = existingParties.FirstOrDefault(e => e.PartyId == p.PartyId);
                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                var filter = Builders<SamPartyDocument>.Filter.Eq(x => x.Id, p.Id);
                return (Filter: filter, Entity: p);
            });

            await _silverPartyRepository.BulkUpsertWithCustomFilterAsync(partyUpserts, cancellationToken);
        }

        var orphanedParties = existingParties
            .Where(e => !incomingPartyIds.Contains(e.PartyId))
            .ToList();

        if (orphanedParties?.Count > 0)
        {
            var deleteFilter = Builders<SamPartyDocument>.Filter.In(
                x => x.Id,
                orphanedParties.Select(d => d.Id)
            );

            await _silverPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
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
            var herdUpserts = incomingHerds.Select(p =>
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

            await _silverHerdRepository.BulkUpsertWithCustomFilterAsync(herdUpserts, cancellationToken);
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

    private async Task UpsertSilverPartyRolesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        incomingSitePartyRoles ??= [];

        var incomingKeys = incomingSitePartyRoles
            .Select(p => $"{p.Source}::{p.HoldingIdentifier}::{p.IsHolder}::{p.PartyId}::{p.RoleTypeId}")
            .ToHashSet();

        var existingSitePartyRoles = await GetExistingSilverSitePartyRoleRelationshipsAsync(
            holdingIdentifier,
            IsHolderPartyType,
            cancellationToken);

        if (incomingSitePartyRoles.Count > 0)
        {
            var holdingUpserts = incomingSitePartyRoles.Select(p =>
            {
                var existing = existingSitePartyRoles.FirstOrDefault(e =>
                    e.Source == p.Source
                    && e.HoldingIdentifier == p.HoldingIdentifier
                    && e.IsHolder == p.IsHolder
                    && e.PartyId == p.PartyId
                    && e.RoleTypeId == p.RoleTypeId);

                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                return (
                    Filter: Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.And(
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, p.Source),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, p.HoldingIdentifier),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.IsHolder, p.IsHolder),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, p.RoleTypeId)
                    ),
                    Entity: p
                );
            });

            await _silverSitePartyRoleRelationshipRepository.BulkUpsertWithCustomFilterAsync(holdingUpserts, cancellationToken);
        }

        var orphanedSitePartyRoles = existingSitePartyRoles?
            .Where(e => !incomingKeys.Contains($"{e.Source}::{e.HoldingIdentifier}::{e.IsHolder}::{e.PartyId}::{e.RoleTypeId}"))
            .ToList() ?? [];

        if (orphanedSitePartyRoles?.Count > 0)
        {
            var deleteFilter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.In(
                x => x.Id,
                orphanedSitePartyRoles.Select(d => d.Id)
            );

            await _silverSitePartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);
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

        // TODO - Look to follow with Silver method
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

    private async Task ReplaceGoldSitePartyRolesAsync(
        string holdingIdentifier,
        List<Core.Documents.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        // TODO - Look to follow with Silver method
        var deleteFilter = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.And(
            Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier)
        );

        await _goldSitePartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);

        if (incomingSitePartyRoles?.Count > 0)
        {
            await _goldSitePartyRoleRelationshipRepository.AddManyAsync(incomingSitePartyRoles, cancellationToken);
        }
    }

    private async Task ReplaceGoldSiteGroupMarksAsync(
        string holdingIdentifier,
        List<SiteGroupMarkRelationshipDocument> incomingSiteGroupMarks,
        CancellationToken cancellationToken)
    {
        // TODO - Look to follow with Silver method
        var deleteFilter = Builders<SiteGroupMarkRelationshipDocument>.Filter.And(
            Builders<SiteGroupMarkRelationshipDocument>.Filter.Eq(x => x.CountyParishHoldingNumber, holdingIdentifier)
        );

        await _goldSiteGroupMarkRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);

        if (incomingSiteGroupMarks?.Count > 0)
        {
            await _goldSiteGroupMarkRelationshipRepository.AddManyAsync(incomingSiteGroupMarks, cancellationToken);
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

    private async Task<List<SamPartyDocument>> GetExistingSilverPartiesAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        var sitePartyIds = await _silverSitePartyRoleRelationshipRepository.FindPartyIdsByHoldingIdentifierAsync(
            holdingIdentifier,
            SourceSystemType.SAM.ToString(),
            IsHolderPartyType,
            cancellationToken);

        if (sitePartyIds.Count == 0)
            return [];

        var partiesFilter = Builders<SamPartyDocument>.Filter.In(x => x.PartyId, sitePartyIds);

        return await _silverPartyRepository.FindAsync(
            partiesFilter,
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

    private async Task<List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>> GetExistingSilverSitePartyRoleRelationshipsAsync(
        string holdingIdentifier,
        bool isHolder,
        CancellationToken cancellationToken)
    {
        var sourceAsSam = SourceSystemType.SAM.ToString();

        return await _silverSitePartyRoleRelationshipRepository.FindAsync(
            x => x.HoldingIdentifier == holdingIdentifier
                && x.Source == sourceAsSam
                && x.IsHolder == isHolder,
            cancellationToken) ?? [];
    }
}