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
    IPremiseActivityTypeLookupService premiseActivityTypeLookupService,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamHoldingImportGoldMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        context.GoldSiteGroupMarks = SiteGroupMarkMapper.ToGold(
            context.SilverHerds,
            context.SilverPartyRoles,
            context.Cph,
            HoldingIdentifierType.CphNumber.ToString());

        context.GoldParties = await SamPartyMapper.ToGold(
            context.SilverParties,
            context.GoldSiteGroupMarks,
            goldPartyRepository,
            countryIdentifierLookupService.GetByIdAsync,
            speciesTypeLookupService.GetByIdAsync,
            cancellationToken);

        context.GoldSite = await SamHoldingMapper.ToGold(
            context.SilverHoldings,
            context.GoldSiteGroupMarks,
            context.GoldParties, // TODO - Does this include the holder? If not, we should find them
            goldSiteRepository,
            countryIdentifierLookupService.GetByIdAsync,
            premiseTypeLookupService.GetByIdAsync,
            speciesTypeLookupService.FindAsync,
            premiseActivityTypeLookupService.FindAsync,
            cancellationToken);

        context.GoldSitePartyRoles = SitePartyRoleMapper.ToGold(
            context.SilverParties,
            context.GoldSiteGroupMarks,
            HoldingIdentifierType.CphNumber.ToString(),
            context.Cph);
    }
}