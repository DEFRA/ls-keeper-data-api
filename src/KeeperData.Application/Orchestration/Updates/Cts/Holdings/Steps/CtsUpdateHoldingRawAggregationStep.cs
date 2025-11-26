using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;

[StepOrder(1)]
public class CtsUpdateHoldingRawAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsUpdateHoldingRawAggregationStep> logger) : UpdateStepBase<CtsUpdateHoldingContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(CtsUpdateHoldingContext context, CancellationToken cancellationToken)
    {
        var getHoldingsTask = _dataBridgeClient.GetCtsHoldingsAsync(context.Cph, cancellationToken);
        var getAgentsTask = _dataBridgeClient.GetCtsAgentsAsync(context.Cph, cancellationToken);
        var getKeepersTask = _dataBridgeClient.GetCtsKeepersAsync(context.Cph, cancellationToken);

        await Task.WhenAll(getHoldingsTask, getAgentsTask, getKeepersTask);

        // Take the single holding from the result list
        context.RawHolding = getHoldingsTask.Result.FirstOrDefault();
        context.RawAgents = getAgentsTask.Result;
        context.RawKeepers = getKeepersTask.Result;
    }
}