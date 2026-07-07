using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(2)]
public class SamHoldingImportSilverMappingStep(
    ISiteActivityTypeLookupService siteActivityTypeLookupService,
    ISiteTypeLookupService siteTypeLookupService,
    IRoleTypeLookupService roleTypeLookupService,
    ICountryIdentifierLookupService countryIdentifierLookupService,
    IProductionUsageLookupService productionUsageLookupService,
    ISpeciesTypeLookupService speciesTypeLookupService,
    ILogger<SamHoldingImportSilverMappingStep> logger)
    : ImportStepBase<SamHoldingImportContext>(logger)
{
    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Silver mapping: {Count} raw holding(s) received for CPH {Cph}", context.RawHoldings?.Count ?? 0, context.Cph);

        context.SilverHoldings = await SamHoldingMapper.ToSilver(
            context.RawHoldings,
            siteActivityTypeLookupService.FindAsync,
            siteTypeLookupService.FindAsync,
            countryIdentifierLookupService.FindAsync,
            cancellationToken);

        var commonLandHoldings = await SamCommonLandMapper.ToSilver(
            context.RawCommonLandsByCommonCph,
            countryIdentifierLookupService.FindAsync,
            cancellationToken,
            logger);
        if (commonLandHoldings.Count > 0)
        {
            context.SilverHoldings.AddRange(commonLandHoldings);
        }

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

        var silverPorts = await SamPortMapper.ToSilver(context.RawPorts,
            countryIdentifierLookupService.FindAsync,
            cancellationToken);
        if (silverPorts.Count > 0)
        {
            logger.LogInformation("Mapped {Count} port(s) to silver for CPH {Cph}", silverPorts.Count, context.Cph);
        }
        context.SilverHoldings.AddRange(silverPorts);

        logger.LogInformation("Silver mapping: {Count} silver holding(s) produced for CPH {Cph}", context.SilverHoldings.Count, context.Cph);
    }
}