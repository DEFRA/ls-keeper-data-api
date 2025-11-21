using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holders.Steps;

[StepOrder(3)]
public class SamHolderImportGoldMappingStep(
    ICountryIdentifierLookupService countryIdentifierLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    IGenericRepository<PartyDocument> goldPartyRepository,
    ILogger<SamHolderImportGoldMappingStep> logger)
    : ImportStepBase<SamHolderImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHolderImportContext context, CancellationToken cancellationToken)
    {
        context.GoldParties = await SamPartyMapper.ToGold(
            context.CurrentDateTime,
            context.SilverParties,
            goldSiteGroupMarks: [], // TODO - Consider lookups for these
            goldPartyRepository,
            countryIdentifierLookupService.GetByIdAsync,
            speciesTypeLookupService.GetByIdAsync,
            cancellationToken);

        context.GoldSitePartyRoles = SitePartyRoleMapper.ToGold(
            context.SilverParties,
            goldSiteGroupMarks: [], // TODO - Consider lookups for these
            HoldingIdentifierType.CphNumber.ToString());
    }
}