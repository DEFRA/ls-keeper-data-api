using KeeperData.Application.Orchestration.Sam.Holdings.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holders.Steps;

[StepOrder(3)]
public class SamHolderImportGoldMappingStep(
    ICountryIdentifierLookupService countryIdentifierLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    ILogger<SamHolderImportGoldMappingStep> logger)
    : ImportStepBase<SamHolderImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHolderImportContext context, CancellationToken cancellationToken)
    {
        // TODO - Add Gold in
        context.GoldParties = await SamPartyMapper.ToGold(
            context.CurrentDateTime,
            context.SilverParties,
            goldSiteGroupMarks: [],
            countryIdentifierLookupService.GetByIdAsync,
            speciesTypeLookupService.GetByIdAsync,
            cancellationToken);

        // TODO - Add Gold in
        context.GoldSitePartyRoles = SitePartyRoleMapper.ToGold(
            context.CurrentDateTime,
            context.GoldParties,
            goldSiteGroupMarks: []);
    }
}
