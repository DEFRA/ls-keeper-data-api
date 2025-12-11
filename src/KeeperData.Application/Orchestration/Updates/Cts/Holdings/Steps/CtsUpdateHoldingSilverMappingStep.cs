using KeeperData.Application.Orchestration.Imports.Cts.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;

[StepOrder(2)]
public class CtsUpdateHoldingSilverMappingStep(
    IRoleTypeLookupService roleTypeLookupService,
    ILogger<CtsUpdateHoldingSilverMappingStep> logger)
    : UpdateStepBase<CtsUpdateHoldingContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsUpdateHoldingContext context, CancellationToken cancellationToken)
    {
        if (context.RawHolding != null)
        {
            context.SilverHolding = CtsHoldingMapper.ToSilver(
                context.RawHolding);
        }

        context.SilverParties = [
            .. await CtsAgentOrKeeperMapper.ToSilver(
                context.RawAgents,
                InferredRoleType.Agent,
                roleTypeLookupService.FindAsync,
                cancellationToken),

            .. await CtsAgentOrKeeperMapper.ToSilver(
                context.RawKeepers,
                InferredRoleType.LivestockKeeper,
                roleTypeLookupService.FindAsync,
                cancellationToken)
        ];

        context.SilverPartyRoles = CtsPartyRoleRelationshipMapper.ToSilver(context.SilverParties);
    }
}