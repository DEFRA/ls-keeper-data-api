using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Updates.Cts.Keepers.Steps;

[StepOrder(3)]
public class CtsUpdateKeeperPersistenceStep(
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    IGenericRepository<SitePartyRoleRelationshipDocument> silverPartyRoleRelationshipRepository,
    ILogger<CtsUpdateKeeperPersistenceStep> logger)
    : UpdateStepBase<CtsUpdateKeeperContext>(logger)
{
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;
    private readonly IGenericRepository<SitePartyRoleRelationshipDocument> _silverPartyRoleRelationshipRepository = silverPartyRoleRelationshipRepository;

    protected override async Task ExecuteCoreAsync(CtsUpdateKeeperContext context, CancellationToken cancellationToken)
    {
        if (context.SilverParty != null)
        {
            await UpsertSilverPartyAsync(context.SilverParty, cancellationToken);
        }

        if (context.SilverPartyRoles.Count > 0)
        {
            await UpsertSilverPartyRolesAsync(context.SilverPartyRoles, cancellationToken);
        }
    }

    private async Task UpsertSilverPartyAsync(CtsPartyDocument incomingParty, CancellationToken cancellationToken)
    {
        var existingParty = await _silverPartyRepository.FindOneAsync(
            x => x.PartyId == incomingParty.PartyId &&
                 x.CountyParishHoldingNumber == incomingParty.CountyParishHoldingNumber,
            cancellationToken);

        incomingParty.Id = existingParty?.Id ?? Guid.NewGuid().ToString();

        if (existingParty != null)
        {
            await _silverPartyRepository.UpdateAsync(incomingParty, cancellationToken);
        }
        else
        {
            await _silverPartyRepository.AddAsync(incomingParty, cancellationToken);
        }
    }

    private async Task UpsertSilverPartyRolesAsync(
        List<SitePartyRoleRelationshipDocument> incomingPartyRoles,
        CancellationToken cancellationToken)
    {
        foreach (var role in incomingPartyRoles)
        {
            var filter = Builders<SitePartyRoleRelationshipDocument>.Filter.And(
                Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.PartyId, role.PartyId),
                Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, role.HoldingIdentifier),
                Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, role.RoleTypeId),
                Builders<SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, role.Source)
            );

            var existingRole = await _silverPartyRoleRelationshipRepository.FindOneByFilterAsync(filter, cancellationToken);

            role.Id = existingRole?.Id ?? Guid.NewGuid().ToString();

            if (existingRole != null)
            {
                await _silverPartyRoleRelationshipRepository.UpdateAsync(role, cancellationToken);
            }
            else
            {
                await _silverPartyRoleRelationshipRepository.AddAsync(role, cancellationToken);
            }
        }
    }
}