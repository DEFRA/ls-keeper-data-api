using KeeperData.Application.Orchestration.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holders.Steps;

[StepOrder(2)]
public class SamHolderImportSilverMappingStep(
    IRoleTypeLookupService roleTypeLookupService,
    ICountryIdentifierLookupService countryIdentifierLookupService,
    ILogger<SamHolderImportSilverMappingStep> logger)
    : ImportStepBase<SamHolderImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHolderImportContext context, CancellationToken cancellationToken)
    {
        context.SilverParties = [
            .. await SamHolderMapper.ToSilver(
                context.CurrentDateTime,
                context.RawHolders,
                InferredRoleType.CphHolder,
                roleTypeLookupService.FindAsync,
                countryIdentifierLookupService.FindAsync,
                cancellationToken)
        ];

        context.SilverPartyRoles = SamPartyRoleRelationshipMapper.ToSilver(
            context.SilverParties,
            HoldingIdentifierType.CphNumber.ToString(),
            holdingIdentifier: null);

        // TODO - Add Gold in
        // context.GoldParties = await SamPartyMapper.ToGold();

        // TODO - Add Gold in
        // context.GoldSitePartyRoles = SitePartyRoleMapper.ToGold(
    }
}