using KeeperData.Application.Extensions;
using KeeperData.Application.Orchestration.Imports.Cts.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Agents.Steps;

[StepOrder(2)]
public class CtsUpdateAgentSilverMappingStep(
    IRoleTypeLookupService roleTypeLookupService,
    ILogger<CtsUpdateAgentSilverMappingStep> logger)
    : UpdateStepBase<CtsUpdateAgentContext>(logger)
{
    protected override async Task ExecuteCoreAsync(CtsUpdateAgentContext context, CancellationToken cancellationToken)
    {
        if (context.RawAgent == null) return;

        var roleName = InferredRoleType.Agent.GetDescription();
        var (roleTypeId, roleTypeName) = await roleTypeLookupService.FindAsync(roleName, cancellationToken);

        context.SilverParty = CtsAgentOrKeeperMapper.ToSilver(
            context.RawAgent,
            HoldingIdentifierType.CphNumber,
            (roleName, roleTypeId, roleTypeName));
    }
}