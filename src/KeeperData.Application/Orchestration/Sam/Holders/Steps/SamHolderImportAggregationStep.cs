using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Holders.Steps;

[StepOrder(1)]
public class SamHolderImportAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<SamHolderImportAggregationStep> logger) : ImportStepBase<SamHolderImportContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(SamHolderImportContext context, CancellationToken cancellationToken)
    {
        context.RawHolders = await _dataBridgeClient.GetSamHoldersByPartyIdAsync(context.PartyId, cancellationToken);
    }
}