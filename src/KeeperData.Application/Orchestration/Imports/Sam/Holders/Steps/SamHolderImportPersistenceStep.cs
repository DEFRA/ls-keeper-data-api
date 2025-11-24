using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holders.Steps;

[StepOrder(4)]
public class SamHolderImportPersistenceStep(
    IGenericRepository<SamPartyDocument> silverPartyRepository,
    ISilverSitePartyRoleRelationshipRepository silverSitePartyRoleRelationshipRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument> goldSitePartyRoleRelationshipRepository,
    ILogger<SamHolderImportPersistenceStep> logger)
    : ImportStepBase<SamHolderImportContext>(logger)
{
    private readonly IGenericRepository<SamPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly ISilverSitePartyRoleRelationshipRepository _silverSitePartyRoleRelationshipRepository = silverSitePartyRoleRelationshipRepository;

    private readonly IGenericRepository<PartyDocument> _goldPartyRepository = goldPartyRepository;
    private readonly IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument> _goldSitePartyRoleRelationshipRepository = goldSitePartyRoleRelationshipRepository;

    protected override async Task ExecuteCoreAsync(SamHolderImportContext context, CancellationToken cancellationToken)
    {
        await UpsertSilverPartiesAsync(context.SilverParties, cancellationToken);

        await UpsertSilverPartyRolesAndDeletePartySpecificOrphansAsync(
            context.SilverParties.Select(x => x.PartyId),
            context.SilverPartyRoles,
            cancellationToken);

        await UpsertGoldPartiesAsync(context.GoldParties, cancellationToken);

        await UpsertGoldPartyRolesAndDeletePartySpecificOrphansAsync(
            context.GoldParties.Select(x => x.CustomerNumber ?? string.Empty).Distinct(),
            context.GoldSitePartyRoles,
            cancellationToken);
    }

    private async Task UpsertSilverPartiesAsync(
        List<SamPartyDocument> incomingParties,
        CancellationToken cancellationToken)
    {
        incomingParties ??= [];

        var upserts = new List<(FilterDefinition<SamPartyDocument> Filter, SamPartyDocument Entity)>();

        foreach (var incoming in incomingParties.Where(p => !string.IsNullOrWhiteSpace(p.PartyId)))
        {
            var existing = await _silverPartyRepository.FindOneAsync(x => x.PartyId == incoming.PartyId, cancellationToken);
            incoming.Id = existing?.Id ?? Guid.NewGuid().ToString();

            var filter = Builders<SamPartyDocument>.Filter.Eq(x => x.PartyId, incoming.PartyId);
            upserts.Add((filter, incoming));
        }

        if (upserts.Count > 0)
        {
            await _silverPartyRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
        }
    }

    private async Task UpsertSilverPartyRolesAndDeletePartySpecificOrphansAsync(
        IEnumerable<string> incomingPartyIds,
        List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        incomingSitePartyRoles ??= [];

        foreach (var partyId in incomingPartyIds)
        {
            var incomingRoles = incomingSitePartyRoles
                .Where(r => r.PartyId == partyId)
                .ToList();

            var existingRoles = await _silverSitePartyRoleRelationshipRepository.FindAsync(
                x => x.PartyId == partyId
                    && x.Source == SourceSystemType.SAM.ToString(),
                cancellationToken) ?? [];

            HashSet<string> incomingKeys = [];

            if (incomingRoles.Count > 0)
            {
                incomingKeys = incomingRoles
                    .Select(p => $"{p.Source}::{p.HoldingIdentifier}::{p.PartyId}::{p.RoleTypeId}")
                    .ToHashSet();

                var upserts = incomingRoles.Select(p =>
                {
                    var existing = existingRoles.FirstOrDefault(e =>
                        e.Source == p.Source &&
                        e.HoldingIdentifier == p.HoldingIdentifier &&
                        e.PartyId == p.PartyId &&
                        e.RoleTypeId == p.RoleTypeId);

                    p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                    var filter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.And(
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, p.Source),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, p.HoldingIdentifier),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, p.RoleTypeId)
                    );

                    return (Filter: filter, Entity: p);
                });

                await _silverSitePartyRoleRelationshipRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
            }

            var orphaned = existingRoles
                .Where(e => !incomingKeys.Contains($"{e.Source}::{e.HoldingIdentifier}::{e.PartyId}::{e.RoleTypeId}"))
                .ToList();

            if (orphaned.Count > 0)
            {
                var deleteFilter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.In(x => x.Id, orphaned.Select(d => d.Id));
                await _silverSitePartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);
            }
        }
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

    private async Task UpsertGoldPartyRolesAndDeletePartySpecificOrphansAsync(
        IEnumerable<string> incomingPartyIds,
        List<Core.Documents.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        incomingSitePartyRoles ??= [];

        foreach (var partyId in incomingPartyIds)
        {
            var incomingRoles = incomingSitePartyRoles
                .Where(r => r.PartyId == partyId)
                .ToList();

            var existingRoles = await _goldSitePartyRoleRelationshipRepository.FindAsync(
                x => x.PartyId == partyId,
                cancellationToken) ?? [];

            HashSet<string> incomingKeys = [];

            if (incomingRoles.Count > 0)
            {
                incomingKeys = incomingRoles
                    .Select(p => $"{p.HoldingIdentifier}::{p.PartyId}::{p.RoleTypeId}::{p.SpeciesTypeId}")
                    .ToHashSet();

                var upserts = incomingRoles.Select(p =>
                {
                    var existing = existingRoles.FirstOrDefault(e =>
                        e.HoldingIdentifier == p.HoldingIdentifier &&
                        e.PartyId == p.PartyId &&
                        e.RoleTypeId == p.RoleTypeId &&
                        e.SpeciesTypeId == p.SpeciesTypeId);

                    p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                    var filter = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.And(
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, p.HoldingIdentifier),
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, p.RoleTypeId),
                        Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.SpeciesTypeId, p.SpeciesTypeId)
                    );

                    return (Filter: filter, Entity: p);
                });

                await _goldSitePartyRoleRelationshipRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
            }

            // TODO - Will this remove roles assigned from SAM Parties?
            var orphaned = existingRoles
                .Where(e => !incomingKeys.Contains($"{e.HoldingIdentifier}::{e.PartyId}::{e.RoleTypeId}::{e.SpeciesTypeId}"))
                .ToList();

            if (orphaned.Count > 0)
            {
                var deleteFilter = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.In(x => x.Id, orphaned.Select(d => d.Id));
                await _goldSitePartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);
            }
        }
    }
}