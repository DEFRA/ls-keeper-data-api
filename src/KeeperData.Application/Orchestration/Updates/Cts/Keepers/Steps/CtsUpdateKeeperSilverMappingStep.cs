using KeeperData.Application.Extensions;
using KeeperData.Application.Orchestration.Imports.Cts.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Keepers.Steps;

[StepOrder(2)]
public class CtsUpdateKeeperSilverMappingStep(
    IRoleTypeLookupService roleTypeLookupService,
    ILogger<CtsUpdateKeeperSilverMappingStep> logger)
    : UpdateStepBase<CtsUpdateKeeperContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsUpdateKeeperContext context, CancellationToken cancellationToken)
    {
        if (context.RawKeeper == null) return;

        var roleName = InferredRoleType.LivestockKeeper.GetDescription();
        var (roleTypeId, roleTypeName) = await roleTypeLookupService.FindAsync(roleName, cancellationToken);

        context.SilverParty = CtsAgentOrKeeperMapper.ToSilver(
            context.RawKeeper,
            HoldingIdentifierType.CPHN,
            (roleName, roleTypeId, roleTypeName));

        context.SilverPartyRoles = CtsPartyRoleRelationshipMapper.ToSilver([context.SilverParty]);
    }
}