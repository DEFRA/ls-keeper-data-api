using KeeperData.Application.Orchestration.Sam.Holdings.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Steps;

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

        context.GoldParties = await SamPartyMapper.ToGold(
            context.CurrentDateTime,
            context.SilverParties,
            countryIdentifierLookupService.GetByIdAsync,
            speciesTypeLookupService.GetByIdAsync,
            cancellationToken);

        // TODO - Add Gold SiteParty
    }
}