using KeeperData.Application.Orchestration.Cts.Holdings.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Holdings.Steps;

[StepOrder(2)]
public class CtsHoldingImportSilverMappingStep(
    IRoleTypeLookupService roleTypeLookupService,
    ILogger<CtsHoldingImportSilverMappingStep> logger)
    : ImportStepBase<CtsHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        context.SilverHoldings = CtsHoldingMapper.ToSilver(context.RawHoldings);

        context.SilverParties = [
            .. await CtsAgentOrKeeperMapper.ToSilver(
                context.RawAgents,
                InferredRoleType.Agent,
                roleTypeLookupService.FindAsync,
                cancellationToken),

            .. await CtsAgentOrKeeperMapper.ToSilver(
                context.RawKeepers,
                InferredRoleType.PrimaryKeeper,
                roleTypeLookupService.FindAsync,
                cancellationToken)
        ];

        context.SilverPartyRoles = CtsPartyRoleRelationshipMapper.ToSilver(
            context.SilverParties,
            context.Cph,
            HoldingIdentifierType.HoldingNumber.ToString());
    }
}