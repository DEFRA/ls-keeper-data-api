using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(3)]
public class SamHoldingImportGoldMappingStep(
    ICountryIdentifierLookupService countryIdentifierLookupService,
    IPremiseTypeLookupService premiseTypeLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    ILogger<SamHoldingImportGoldMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        context.GoldSite = await SamHoldingMapper.ToGold(
            context.CurrentDateTime,
            context.SilverHoldings,
            countryIdentifierLookupService.GetByIdAsync,
            premiseTypeLookupService.GetByIdAsync,
            cancellationToken);

        context.GoldSiteGroupMarks = SiteGroupMarkMapper.ToGold(
            context.CurrentDateTime,
            context.SilverHerds,
            context.Cph,
            HoldingIdentifierType.CphNumber.ToString());

        context.GoldParties = await SamPartyMapper.ToGold(
            context.CurrentDateTime,
            context.SilverParties,
            context.GoldSiteGroupMarks,
            countryIdentifierLookupService.GetByIdAsync,
            speciesTypeLookupService.GetByIdAsync,
            cancellationToken);

        context.GoldSitePartyRoles = SitePartyRoleMapper.ToGold(
            context.CurrentDateTime,
            context.GoldParties,
            context.GoldSiteGroupMarks);

        context.GoldSite = SitePartyRoleMapper.EnrichSiteWithParties(
            context.GoldSite,
            context.GoldParties);

        context.GoldSite = SiteGroupMarkMapper.EnrichSiteWithGroupMarks(
            context.GoldSite,
            context.GoldSiteGroupMarks);
    }
}