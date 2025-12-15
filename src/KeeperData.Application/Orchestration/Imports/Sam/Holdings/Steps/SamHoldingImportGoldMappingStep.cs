using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(3)]
public class SamHoldingImportGoldMappingStep(
    ICountryIdentifierLookupService countryIdentifierLookupService,
    IPremiseTypeLookupService premiseTypeLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    IPremiseActivityTypeLookupService premiseActivityTypeLookupService,
    ISiteIdentifierTypeLookupService siteIdentifierTypeLookupService,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamHoldingImportGoldMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHoldings.Count > 0)
        {
            var representative = context.SilverHoldings.Any(x => x.IsActive)
            ? context.SilverHoldings.Where(x => x.IsActive).OrderByDescending(h => h.LastUpdatedDate).First()
            : context.SilverHoldings.OrderByDescending(h => h.LastUpdatedDate).First();

            var existingHoldingFilter = Builders<SiteDocument>.Filter.ElemMatch(
                x => x.Identifiers,
                i => i.Identifier == representative.CountyParishHoldingNumber);

            var existingSite = await goldSiteRepository.FindOneByFilterAsync(existingHoldingFilter, cancellationToken);
            context.ExistingGoldSite = existingSite;
            context.GoldSiteId = existingSite != null ? existingSite.Id : Guid.NewGuid().ToString();

            context.GoldSiteGroupMarks = SiteGroupMarkMapper.ToGold(
                context.SilverHerds,
                context.SilverPartyRoles,
                context.Cph);

            context.GoldParties = await SamPartyMapper.ToGold(
                context.GoldSiteId,
                context.SilverParties,
                context.GoldSiteGroupMarks,
                goldPartyRepository,
                countryIdentifierLookupService.GetByIdAsync,
                speciesTypeLookupService.GetByIdAsync,
                cancellationToken);

            context.GoldSite = await SamHoldingMapper.ToGold(
                context.GoldSiteId,
                context.ExistingGoldSite,
                context.SilverHoldings,
                context.GoldSiteGroupMarks,
                context.GoldParties,
                countryIdentifierLookupService.GetByIdAsync,
                premiseTypeLookupService.GetByIdAsync,
                siteIdentifierTypeLookupService.GetByCodeAsync,
                speciesTypeLookupService.FindAsync,
                premiseActivityTypeLookupService.GetByCodeAsync,
                cancellationToken);

            context.GoldSitePartyRoles = SitePartyRoleMapper.ToGold(
                context.GoldParties,
                context.GoldSiteGroupMarks,
                context.GoldSiteId,
                context.Cph);

            SamPartyMapper.EnrichPartyRoleWithSiteInformation(
                context.GoldParties,
                context.GoldSite);
        }
    }
}