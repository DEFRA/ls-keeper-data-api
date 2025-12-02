using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Cts.Holdings.Steps;

[StepOrder(4)]
public class CtsHoldingImportPersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument> silverSitePartyRoleRelationshipRepository,
    ILogger<CtsHoldingImportPersistenceStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument> _silverSitePartyRoleRelationshipRepository = silverSitePartyRoleRelationshipRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHoldings?.Count > 0)
        {
            var primaryHolding = context.SilverHoldings[0];
            await UpsertSilverHoldingAsync(primaryHolding, cancellationToken);
        }

        await UpsertSilverPartiesAndDeleteOrphansAsync(context.CphTrimmed, context.SilverParties, cancellationToken);

        await UpsertSilverPartyRolesAndDeletePartySpecificOrphansAsync(
            context.CphTrimmed,
            context.SilverPartyRoles,
            cancellationToken);
    }

    private async Task UpsertSilverHoldingAsync(
        CtsHoldingDocument incomingHolding,
        CancellationToken cancellationToken)
    {
        var existingHolding = await _silverHoldingRepository.FindOneAsync(
            x => x.CountyParishHoldingNumber == incomingHolding.CountyParishHoldingNumber,
            cancellationToken);

        incomingHolding.Id = existingHolding?.Id ?? Guid.NewGuid().ToString();

        var holdingUpsert = (
            Filter: Builders<CtsHoldingDocument>.Filter.Eq(
                x => x.CountyParishHoldingNumber, incomingHolding.CountyParishHoldingNumber),
            Entity: incomingHolding);

        await _silverHoldingRepository.BulkUpsertWithCustomFilterAsync(
            [holdingUpsert], cancellationToken);
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

        var orphanedParties = existingParties?
            .Where(e => !incomingKeys.Contains($"{e.PartyId}::{e.CountyParishHoldingNumber}"))
            .ToList() ?? [];

        if (orphanedParties?.Count > 0)
        {
            var deleteFilter = Builders<CtsPartyDocument>.Filter.In(
                x => x.Id,
                orphanedParties.Select(d => d.Id)
            );

            await _silverPartyRepository.DeleteManyAsync(deleteFilter, cancellationToken);
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

    private async Task UpsertSilverPartyRolesAndDeletePartySpecificOrphansAsync(
        string holdingIdentifier,
        List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> incomingSitePartyRoles,
        CancellationToken cancellationToken)
    {
        incomingSitePartyRoles ??= [];

        var incomingKeys = incomingSitePartyRoles
            .Select(p => $"{p.Source}::{p.HoldingIdentifier}::{p.PartyId}::{p.RoleTypeId}")
            .ToHashSet();

        var existingSitePartyRoles = await GetExistingSilverSitePartyRoleRelationshipsAsync(
            holdingIdentifier,
            cancellationToken);

        if (incomingSitePartyRoles.Count > 0)
        {
            var holdingUpserts = incomingSitePartyRoles.Select(p =>
            {
                var existing = existingSitePartyRoles.FirstOrDefault(e =>
                    e.Source == p.Source &&
                    e.HoldingIdentifier == p.HoldingIdentifier &&
                    e.PartyId == p.PartyId &&
                    e.RoleTypeId == p.RoleTypeId);

                p.Id = existing?.Id ?? Guid.NewGuid().ToString();

                return (
                    Filter: Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.And(
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, p.Source),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, p.HoldingIdentifier),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, p.PartyId),
                        Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, p.RoleTypeId)
                    ),
                    Entity: p
                );
            });

            await _silverSitePartyRoleRelationshipRepository.BulkUpsertWithCustomFilterAsync(holdingUpserts, cancellationToken);
        }

        var orphanedSitePartyRoles = existingSitePartyRoles?.Where(e =>
            !incomingKeys.Contains($"{e.Source}::{e.HoldingIdentifier}::{e.PartyId}::{e.RoleTypeId}"))
        .ToList() ?? [];

        if (orphanedSitePartyRoles.Count > 0)
        {
            var deleteFilter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.In(
                x => x.Id,
                orphanedSitePartyRoles.Select(d => d.Id));

            await _silverSitePartyRoleRelationshipRepository.DeleteManyAsync(deleteFilter, cancellationToken);
        }
    }

    private async Task<List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>> GetExistingSilverSitePartyRoleRelationshipsAsync(
        string holdingIdentifier,
        CancellationToken cancellationToken)
    {
        var sourceAsCts = SourceSystemType.CTS.ToString();

        return await _silverSitePartyRoleRelationshipRepository.FindAsync(
            x => x.HoldingIdentifier == holdingIdentifier
                && x.Source == sourceAsCts,
            cancellationToken) ?? [];
    }
}