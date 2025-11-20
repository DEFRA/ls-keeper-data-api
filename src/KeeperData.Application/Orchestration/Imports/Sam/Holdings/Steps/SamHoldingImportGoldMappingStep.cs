using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(3)]
public class SamHoldingImportGoldMappingStep(
    ICountryIdentifierLookupService countryIdentifierLookupService,
    IPremiseTypeLookupService premiseTypeLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    IProductionUsageLookupService productionUsageLookupService,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamHoldingImportGoldMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        context.GoldSiteGroupMarks = SiteGroupMarkMapper.ToGold(
            context.CurrentDateTime,
            context.SilverHerds,
            context.SilverPartyRoles,
            context.Cph,
            HoldingIdentifierType.CphNumber.ToString());

        context.GoldParties = await SamPartyMapper.ToGold(
            context.CurrentDateTime,
            context.SilverParties,
            context.GoldSiteGroupMarks,
            goldPartyRepository,
            countryIdentifierLookupService.GetByIdAsync,
            speciesTypeLookupService.GetByIdAsync,
            cancellationToken);

        context.GoldSite = await SamHoldingMapper.ToGold(
            context.CurrentDateTime,
            context.SilverHoldings,
            context.GoldSiteGroupMarks,
            context.GoldParties,
            goldSiteRepository,
            countryIdentifierLookupService.GetByIdAsync,
            premiseTypeLookupService.GetByIdAsync,
            speciesTypeLookupService.FindAsync,
            productionUsageLookupService.FindAsync,
            cancellationToken);

        context.GoldSitePartyRoles = SitePartyRoleMapper.ToGold(
            context.CurrentDateTime,
            context.SilverParties,
            context.GoldSiteGroupMarks,
            HoldingIdentifierType.CphNumber.ToString(),
            context.Cph);
    }
}