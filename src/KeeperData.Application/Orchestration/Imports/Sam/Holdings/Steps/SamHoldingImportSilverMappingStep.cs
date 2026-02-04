using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
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
    ISpeciesTypeLookupService speciesTypeLookupService,
    ILogger<SamHoldingImportSilverMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        context.SilverHoldings = await SamHoldingMapper.ToSilver(
            context.RawHoldings,
            premiseActivityTypeLookupService.FindAsync,
            premiseTypeLookupService.FindAsync,
            countryIdentifierLookupService.FindAsync,
            cancellationToken);

        context.SilverParties = [
            .. await SamPartyMapper.ToSilver(
                context.Cph,
                context.RawParties,
                roleTypeLookupService.FindAsync,
                countryIdentifierLookupService.FindAsync,
                cancellationToken)
        ];

        context.SilverPartyRoles = SamPartyRoleRelationshipMapper.ToSilver(
            context.SilverParties,
            context.Cph);

        context.SilverHerds = await SamHerdMapper.ToSilver(
            context.RawHerds,
            productionUsageLookupService.FindAsync,
            speciesTypeLookupService.FindAsync,
            cancellationToken);
    }
}