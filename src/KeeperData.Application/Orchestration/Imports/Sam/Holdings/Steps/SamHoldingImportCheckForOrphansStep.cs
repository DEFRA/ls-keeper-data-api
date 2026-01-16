using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(4)]
public class SamHoldingImportCheckForOrphansStep(
    IGoldSitePartyRoleRelationshipRepository goldSitePartyRoleRelationshipRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamHoldingImportCheckForOrphansStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    // TODO - Add tests for SamHoldingImportCheckForOrphansStep
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        var incomingPartyIds = context.GoldParties
            .Select(x => x.CustomerNumber)
            .Distinct()
            .ToList();

        var existingRelationships = await goldSitePartyRoleRelationshipRepository.GetExistingSitePartyRoleRelationships(
            context.Cph,
            cancellationToken);

        var orphans = existingRelationships
            .Where(er => !incomingPartyIds.Any(ir =>
                ir == er.CustomerNumber))
            .ToList();

        context.PartiesWithNoRelationshipToSiteToClean = orphans;

        if (orphans.Count > 0)
        {
            var cleanedParties = await SamPartyMapper.RemoveSitePartyOrphans(
                context.GoldSiteId,
                context.PartiesWithNoRelationshipToSiteToClean,
                goldPartyRepository,
                cancellationToken);

            context.GoldParties ??= [];
            context.GoldParties.AddRange(cleanedParties);
        }
    }
}