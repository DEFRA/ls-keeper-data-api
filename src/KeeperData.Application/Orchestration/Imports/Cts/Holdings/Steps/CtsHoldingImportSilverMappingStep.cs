using KeeperData.Application.Orchestration.Imports.Cts.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Cts.Holdings.Steps;

[StepOrder(2)]
public class CtsHoldingImportSilverMappingStep(
    IRoleTypeLookupService roleTypeLookupService,
    ILogger<CtsHoldingImportSilverMappingStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        context.SilverHoldings = CtsHoldingMapper.ToSilver(
            context.CurrentDateTime,
            context.RawHoldings);

        context.SilverParties = [
            .. await CtsAgentOrKeeperMapper.ToSilver(
                context.CurrentDateTime,
                context.RawAgents,
                HoldingIdentifierType.CphNumber,
                InferredRoleType.Agent,
                roleTypeLookupService.FindAsync,
                cancellationToken),

            .. await CtsAgentOrKeeperMapper.ToSilver(
                context.CurrentDateTime,
                context.RawKeepers,
                HoldingIdentifierType.CphNumber,
                InferredRoleType.LivestockKeeper,
                roleTypeLookupService.FindAsync,
                cancellationToken)
        ];

        context.SilverPartyRoles = CtsPartyRoleRelationshipMapper.ToSilver(context.SilverParties);
    }
}