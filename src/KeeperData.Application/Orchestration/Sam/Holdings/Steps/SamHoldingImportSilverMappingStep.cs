using KeeperData.Application.Orchestration.Sam.Holdings.Mappings;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Steps;

[StepOrder(2)]
public class SamHoldingImportSilverMappingStep(
    IPremiseActivityTypeLookupService premiseActivityTypeLookupService,
    IPremiseTypeLookupService premiseTypeLookupService,
    ICountryIdentifierLookupService countryIdentifierLookupService,
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
            .. await SamHolderMapper.ToSilver(
                context.RawHolders,
                cancellationToken),

            .. await SamPartyMapper.ToSilver(
                context.RawParties,
                context.RawHerds,
                cancellationToken)
        ];

        context.SilverPartyRoles = SamPartyRoleRelationshipMapper.ToSilver(
            context.SilverParties,
            context.Cph,
            HoldingIdentifierType.HoldingNumber.ToString());
    }
}