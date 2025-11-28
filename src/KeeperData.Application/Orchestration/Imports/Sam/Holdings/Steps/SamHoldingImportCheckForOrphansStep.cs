using KeeperData.Application.Extensions;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Working;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(4)]
public class SamHoldingImportCheckForOrphansStep(IGoldSitePartyRoleRelationshipRepository goldSitePartyRoleRelationshipRepository,
    IRoleTypeLookupService roleTypeLookupService,
    ILogger<SamHoldingImportCheckForOrphansStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        var findHolderRole = await roleTypeLookupService.FindAsync(InferredRoleType.CphHolder.GetDescription(), cancellationToken);

        var holderPartyIds = context.RawHolders.Select(x => x.PARTY_ID.Trim()).ToList();

        var incomingRelationships = context.RawHolders
            .SelectMany(holder => holder.CphList.Select(cph => new SitePartyRoleRelationship
            {
                PartyId = holder.PARTY_ID.Trim(),
                HoldingIdentifier = cph.Trim()
            }))
            .Distinct()
            .ToList();

        var existingRelationships = await goldSitePartyRoleRelationshipRepository.GetExistingSitePartyRoleRelationships(
            holderPartyIds,
            findHolderRole.roleTypeId ?? string.Empty,
            cancellationToken);

        var orphans = existingRelationships
            .Where(er => !incomingRelationships.Any(ir =>
                ir.PartyId == er.PartyId &&
                ir.HoldingIdentifier == er.HoldingIdentifier))
            .ToList();

        context.SiteHolderPartyOrphansToClean = orphans;
    }
}
