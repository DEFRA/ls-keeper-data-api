using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(2)]
public class SamHoldingImportSilverMappingStep(
    IPremiseActivityTypeLookupService premiseActivityTypeLookupService,
    IPremiseTypeLookupService premiseTypeLookupService,
    IRoleTypeLookupService roleTypeLookupService,
    ICountryIdentifierLookupService countryIdentifierLookupService,
    IProductionUsageLookupService productionUsageLookupService,
    // IProductionTypeLookupService productionTypeLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    ILogger<SamHoldingImportSilverMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        context.SilverHoldings = await SamHoldingMapper.ToSilver(
            context.CurrentDateTime,
            context.RawHoldings,
            premiseActivityTypeLookupService.FindAsync,
            premiseTypeLookupService.FindAsync,
            countryIdentifierLookupService.FindAsync,
            cancellationToken);

        context.SilverParties = [
            .. await SamPartyMapper.ToSilver(
                context.CurrentDateTime,
                context.RawParties,
                roleTypeLookupService.FindAsync,
                countryIdentifierLookupService.FindAsync,
                cancellationToken)
        ];

        context.SilverPartyRoles = SamPartyRoleRelationshipMapper.ToSilver(
            context.SilverParties,
            HoldingIdentifierType.CphNumber.ToString(),
            context.Cph);

        context.SilverHerds = await SamHerdMapper.ToSilver(
            context.CurrentDateTime,
            context.RawHerds,
            productionUsageLookupService.FindAsync,
            // productionTypeLookupService.FindAsync,
            speciesTypeLookupService.FindAsync,
            cancellationToken);

        context.SilverHoldings = SamHoldingGroupMarkMapper.EnrichHoldingsWithGroupMarks(
            context.SilverHoldings,
            context.SilverHerds);
    }
}