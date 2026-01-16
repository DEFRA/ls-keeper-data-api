using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Extensions;
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
            await UpsertGoldSiteAsync(
                context.ExistingGoldSite == null,
                context.GoldSite,
                cancellationToken);
        }

        await UpsertGoldPartiesAsync(context.ExistingGoldPartyIds, context.GoldParties, cancellationToken);

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

        var newItems = new List<SamHoldingDocument>();
        var updateItems = new List<(FilterDefinition<SamHoldingDocument> Filter, UpdateDefinition<SamHoldingDocument> Update)>();

        foreach (var incoming in incomingHoldings)
        {
            var existing = existingHoldings.FirstOrDefault(e =>
                e.CountyParishHoldingNumber == incoming.CountyParishHoldingNumber
                && e.LocationName == incoming.LocationName
                && e.SpeciesTypeCode == incoming.SpeciesTypeCode
                && e.SecondaryCph == incoming.SecondaryCph);

            if (existing is null)
            {
                incoming.Id = Guid.NewGuid().ToString();
                newItems.Add(incoming);
            }
            else
            {
                incoming.Id = existing.Id;

                var filter = Builders<SamHoldingDocument>.Filter.Eq(x => x.Id, incoming.Id);
                var update = Builders<SamHoldingDocument>.Update.SetAll(incoming);
                updateItems.Add((filter, update));
            }
        }

        if (newItems.Count > 0)
            await _silverHoldingRepository.AddManyAsync(newItems, cancellationToken);

        if (updateItems.Count > 0)
            await _silverHoldingRepository.BulkUpdateWithCustomFilterAsync(updateItems, cancellationToken);

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
            .ToList();

        var existingParties = await _silverPartyRepository.FindAsync(
            x => incomingPartyIds.Contains(x.PartyId),
            cancellationToken);

        existingParties ??= [];

        var existingPartiesById = existingParties.ToDictionary(p => p.PartyId);

        var newItems = new List<SamPartyDocument>();
        var updateItems = new List<(FilterDefinition<SamPartyDocument> Filter, UpdateDefinition<SamPartyDocument> Update)>();

        foreach (var incoming in incomingParties)
        {
            if (string.IsNullOrWhiteSpace(incoming.PartyId))
                continue;

            existingPartiesById.TryGetValue(incoming.PartyId, out var existing);

            if (existing is null)
            {
                incoming.Id = Guid.NewGuid().ToString();
                newItems.Add(incoming);
            }
            else
            {
                incoming.Id = existing.Id;

                var filter = Builders<SamPartyDocument>.Filter.Eq(x => x.Id, incoming.Id);
                var update = Builders<SamPartyDocument>.Update.SetAll(incoming);
                updateItems.Add((filter, update));
            }
        }

        if (newItems.Count > 0)
            await _silverPartyRepository.AddManyAsync(newItems, cancellationToken);

        if (updateItems.Count > 0)
            await _silverPartyRepository.BulkUpdateWithCustomFilterAsync(updateItems, cancellationToken);
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

        var newItems = new List<SamHerdDocument>();
        var updateItems = new List<(FilterDefinition<SamHerdDocument> Filter, UpdateDefinition<SamHerdDocument> Update)>();

        foreach (var incoming in incomingHerds)
        {
            var existing = existingHerds.FirstOrDefault(e =>
                e.CountyParishHoldingHerd == incoming.CountyParishHoldingHerd &&
                e.ProductionUsageCode == incoming.ProductionUsageCode &&
                e.Herdmark == incoming.Herdmark);

            if (existing is null)
            {
                incoming.Id = Guid.NewGuid().ToString();
                newItems.Add(incoming);
            }
            else
            {
                incoming.Id = existing.Id;

                var filter = Builders<SamHerdDocument>.Filter.Eq(x => x.Id, incoming.Id);
                var update = Builders<SamHerdDocument>.Update.SetAll(incoming);
                updateItems.Add((filter, update));
            }
        }

        if (newItems.Count > 0)
            await _silverHerdRepository.AddManyAsync(newItems, cancellationToken);

        if (updateItems.Count > 0)
            await _silverHerdRepository.BulkUpdateWithCustomFilterAsync(updateItems, cancellationToken);

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
        bool isInsert,
        SiteDocument incomingSite,
        CancellationToken cancellationToken)
    {
        if (isInsert)
        {
            await _goldSiteRepository.AddAsync(incomingSite, cancellationToken);
        }
        else
        {
            await _goldSiteRepository.UpdateAsync(incomingSite, cancellationToken);
        }
    }

    private async Task UpsertGoldPartiesAsync(
        List<string> existingGoldPartyIds,
        List<PartyDocument> incomingParties,
        CancellationToken cancellationToken)
    {
        incomingParties ??= [];

        var incomingCustomerNumbers = incomingParties
            .Select(p => p.CustomerNumber)
            .Where(cn => !string.IsNullOrWhiteSpace(cn))
            .ToList();

        var newItems = new List<PartyDocument>();
        var updateItems = new List<(FilterDefinition<PartyDocument> Filter, UpdateDefinition<PartyDocument> Update)>();

        foreach (var incoming in incomingParties)
        {
            if (!existingGoldPartyIds.Contains(incoming.Id))
            {
                newItems.Add(incoming);
            }
            else
            {
                var filter = Builders<PartyDocument>.Filter.Eq(x => x.Id, incoming.Id);
                var update = Builders<PartyDocument>.Update.SetAll(incoming);
                updateItems.Add((filter, update));
            }
        }

        if (newItems.Count > 0)
            await _goldPartyRepository.AddManyAsync(newItems, cancellationToken);

        if (updateItems.Count > 0)
            await _goldPartyRepository.BulkUpdateWithCustomFilterAsync(updateItems, cancellationToken);
    }

    private async Task UpsertGoldPartyRolesAndDeleteOrphansAsync(
        string holdingIdentifier,
        List<Core.Documents.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        incomingSitePartyRoles ??= [];

        var incomingKeys = incomingSitePartyRoles
            .Select(p => $"{p.HoldingIdentifier}::{p.CustomerNumber}::{p.RoleTypeId}::{p.SpeciesTypeId}")
            .ToHashSet();

        var existingSitePartyRoles = await GetExistingGoldSitePartyRoleRelationshipsAsync(
            holdingIdentifier,
            cancellationToken);

        var newItems = new List<Core.Documents.SitePartyRoleRelationshipDocument>();
        var updateItems = new List<(FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument> Filter, UpdateDefinition<Core.Documents.SitePartyRoleRelationshipDocument> Update)>();

        foreach (var incoming in incomingSitePartyRoles)
        {
            var existing = existingSitePartyRoles.FirstOrDefault(e =>
                e.CustomerNumber == incoming.CustomerNumber &&
                e.HoldingIdentifier == incoming.HoldingIdentifier &&
                e.RoleTypeId == incoming.RoleTypeId &&
                e.SpeciesTypeId == incoming.SpeciesTypeId);

            if (existing is null)
            {
                incoming.Id = Guid.NewGuid().ToString();
                newItems.Add(incoming);
            }
            else
            {
                incoming.Id = existing.Id;

                var filter = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Id, incoming.Id);
                var update = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Update.SetAll(incoming);
                updateItems.Add((filter, update));
            }
        }

        if (newItems.Count > 0)
            await _goldSitePartyRoleRelationshipRepository.AddManyAsync(newItems, cancellationToken);

        if (updateItems.Count > 0)
            await _goldSitePartyRoleRelationshipRepository.BulkUpdateWithCustomFilterAsync(updateItems, cancellationToken);

        var orphanedSitePartyRoles = existingSitePartyRoles?.Where(e =>
            !incomingKeys.Contains($"{e.HoldingIdentifier}::{e.CustomerNumber}::{e.RoleTypeId}::{e.SpeciesTypeId}"))
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