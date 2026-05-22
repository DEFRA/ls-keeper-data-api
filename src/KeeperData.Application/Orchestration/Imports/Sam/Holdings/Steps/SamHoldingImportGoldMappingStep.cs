using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(3)]
public class SamHoldingImportGoldMappingStep(
    ICountryIdentifierLookupService countryIdentifierLookupService,
    ISiteTypeLookupService siteTypeLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    ISiteActivityTypeLookupService siteActivityTypeLookupService,
    ISiteIdentifierTypeLookupService siteIdentifierTypeLookupService,
    ISiteTypeDerivedCodeLookupService siteTypeDerivedCodeLookupService,
    IGenericRepository<SiteDocument> goldSiteRepository,
    IPartiesRepository goldPartyRepository,
    ILogger<SamHoldingImportGoldMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        if (context.SilverHoldings.Count > 0)
        {
            var representative = context.SilverHoldings.Any(x => x.HoldingStatus == HoldingStatusType.Active.GetDescription())
            ? context.SilverHoldings.Where(x => x.HoldingStatus == HoldingStatusType.Active.GetDescription()).OrderByDescending(h => h.LastUpdatedDate).First()
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
                context.ExistingGoldPartyIds,
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
                siteTypeLookupService.GetByCodeAsync,
                siteIdentifierTypeLookupService.GetByCodeAsync,
                speciesTypeLookupService.FindAsync,
                siteActivityTypeLookupService.GetByCodeAsync,
                siteTypeDerivedCodeLookupService,
                cancellationToken);

            await EnrichWithCommonLandDataAsync(context, representative, cancellationToken);

            logger.LogInformation("Associated main sites queued for update: {Count} for CPH {Cph}",
                context.AssociatedMainSites?.Count ?? 0, context.Cph);

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

    private async Task EnrichWithCommonLandDataAsync(SamHoldingImportContext context, SamHoldingDocument representative, CancellationToken cancellationToken)
    {
        var goldSite = context.GoldSite;
        if (goldSite == null) return;

        goldSite.LocalAuthorityName = representative.LocalAuthorityName;

        goldSite.AssociatedMainHoldings = representative.AssociatedMainHoldings
            .Select(r => new AssociatedHoldingDocument
            {
                HoldingIdentifier = r.HoldingIdentifier,
                ContiguousFlag = r.ContiguousFlag,
                StartDate = r.StartDate,
                EndDate = r.EndDate
            })
            .ToList();

        if (goldSite.AssociatedMainHoldings?.Count > 0)
        {
            await FindAndUpdateMainSiteIfExists(context, representative, goldSite.AssociatedMainHoldings, cancellationToken);
        }
    }

    private async Task FindAndUpdateMainSiteIfExists(SamHoldingImportContext context, SamHoldingDocument representative, List<AssociatedHoldingDocument> mainHoldings, CancellationToken cancellationToken)
    {
        foreach (var mainHolding in mainHoldings)
        {
            if (string.IsNullOrWhiteSpace(mainHolding.HoldingIdentifier))
                continue;

            var filter = Builders<SiteDocument>.Filter.ElemMatch(
                x => x.Identifiers,
                i => i.Identifier == mainHolding.HoldingIdentifier);

            var mainSite = await goldSiteRepository.FindOneByFilterAsync(filter, cancellationToken);

            if (mainSite is null)
            {
                logger.LogDebug("No main site found for identifier {Identifier}", mainHolding.HoldingIdentifier);
                continue;
            }

            logger.LogInformation("Found main site {SiteId} for identifier {Identifier}", mainSite.Id, mainHolding.HoldingIdentifier);

            var commonForMain = new AssociatedHoldingDocument
            {
                HoldingIdentifier = representative.CountyParishHoldingNumber,
                ContiguousFlag = mainHolding.ContiguousFlag,
                StartDate = mainHolding.StartDate,
                EndDate = mainHolding.EndDate
            };

            // Ensure the main site's AssociatedCommonLands list exists
            mainSite.AssociatedCommonLands ??= new List<AssociatedHoldingDocument>();

            // Only add the common land entry if it does not already exist
            if (!mainSite.AssociatedCommonLands.Any(a => a.HoldingIdentifier == commonForMain.HoldingIdentifier))
            {
                mainSite.AssociatedCommonLands.Add(commonForMain);
            }

            // Ensure the main site is present in the context so the persistence step can operate on it
            context.AssociatedMainSites ??= new List<SiteDocument>();
            var existingIndex = context.AssociatedMainSites.FindIndex(s => s.Id == mainSite.Id);
            if (existingIndex >= 0)
                context.AssociatedMainSites[existingIndex] = mainSite;
            else
                context.AssociatedMainSites.Add(mainSite);
        }
    }
}