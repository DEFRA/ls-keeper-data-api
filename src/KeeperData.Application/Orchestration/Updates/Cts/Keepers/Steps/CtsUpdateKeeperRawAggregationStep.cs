using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Keepers.Steps;

[StepOrder(1)]
public class CtsUpdateKeeperRawAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsUpdateKeeperRawAggregationStep> logger) : UpdateStepBase<CtsUpdateKeeperContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(CtsUpdateKeeperContext context, CancellationToken cancellationToken)
    {
        context.RawKeeper = await _dataBridgeClient.GetCtsKeeperByPartyIdAsync(context.PartyId, cancellationToken);
    }
}