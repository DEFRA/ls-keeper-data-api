using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Sam.Holders.Steps;

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

    private const bool IsHolderPartyType = true;

    protected override async Task ExecuteCoreAsync(SamHolderImportContext context, CancellationToken cancellationToken)
    {
        await UpsertSilverPartiesAndDeleteOrphansAsync(context.SilverParties, cancellationToken);

        await UpsertSilverPartyRolesAndDeleteOrphansAsync(
            context.SilverParties.Select(x => x.PartyId),
            context.SilverPartyRoles,
            cancellationToken);

        // TODO - Add Gold in
        // await UpsertGoldPartiesAndDeleteOrphansAsync(context.Cph, context.GoldParties, cancellationToken);

        // TODO - Add Gold in
        // await ReplaceGoldSitePartyRolesAsync(context.Cph, context.GoldSitePartyRoles, cancellationToken);
    }

    /// <summary>
    /// There should only be a single Holder Party.
    /// </summary>
    /// <param name="incomingParties"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task UpsertSilverPartiesAndDeleteOrphansAsync(
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

    private async Task UpsertSilverPartyRolesAndDeleteOrphansAsync(
        IEnumerable<string> incomingPartyIds,
        List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        incomingSitePartyRoles ??= [];

        foreach(var partyId in incomingPartyIds)
        {
            var incomingRoles = incomingSitePartyRoles
                .Where(r => r.PartyId == partyId)
                .ToList();

            var existingRoles = await _silverSitePartyRoleRelationshipRepository.FindAsync(
                    x => x.PartyId == partyId
                        && x.Source == SourceSystemType.SAM.ToString()
                        && x.IsHolder == IsHolderPartyType,
                    cancellationToken) ?? [];

            HashSet<string> incomingKeys = [];

            if (incomingRoles.Count > 0)
            {
                incomingKeys = incomingRoles
                    .Select(p => $"{p.Source}::{p.HoldingIdentifier}::{p.IsHolder}::{p.PartyId}::{p.RoleTypeId}")
                    .ToHashSet();

                var upserts = incomingRoles.Select(p =>
                {
                    var existing = existingRoles.FirstOrDefault(e =>
                        e.Source == p.Source &&
                        e.HoldingIdentifier == p.HoldingIdentifier &&
                        e.IsHolder == p.IsHolder &&
                        e.PartyId == p.PartyId &&
                        e.RoleTypeId == p.RoleTypeId);

                    p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                    var filter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.And(
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, p.Source),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, p.HoldingIdentifier),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.IsHolder, p.IsHolder),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, p.RoleTypeId)
                    );

                    return (Filter: filter, Entity: p);
                });

                await _silverSitePartyRoleRelationshipRepository.BulkUpsertWithCustomFilterAsync(upserts, cancellationToken);
            }

            var orphaned = existingRoles
                .Where(e => !incomingKeys.Contains($"{e.Source}::{e.HoldingIdentifier}::{e.IsHolder}::{e.PartyId}::{e.RoleTypeId}"))
                .ToList();

            if (orphaned.Count > 0)
            {
                var deleteFilter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.In(x => x.Id, orphaned.Select(d => d.Id));
                await _silverSitePartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);
            }
        }
    }

    // TODO - Add Gold in
    // await UpsertGoldPartiesAndDeleteOrphansAsync(context.Cph, context.GoldParties, cancellationToken);

    // TODO - Add Gold in
    // await ReplaceGoldSitePartyRolesAsync(context.Cph, context.GoldSitePartyRoles, cancellationToken);
}
