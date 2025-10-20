using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(1)]
public class CtsHoldingInsertRawAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsHoldingInsertRawAggregationStep> logger) : ImportStepBase<CtsHoldingInsertContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(CtsHoldingInsertContext context, CancellationToken cancellationToken)
    {
        var getHoldingsTask = _dataBridgeClient.GetCtsHoldingsAsync(context.Cph, cancellationToken);
        var getAgentsTask = _dataBridgeClient.GetCtsAgentsAsync(context.Cph, cancellationToken);
        var getKeepersTask = _dataBridgeClient.GetCtsKeepersAsync(context.Cph, cancellationToken);

        await Task.WhenAll(
            getHoldingsTask,
            getAgentsTask,
            getKeepersTask);

        context.RawHoldings = getHoldingsTask.Result;

        context.RawAgents = getAgentsTask.Result;

        context.RawKeepers = getKeepersTask.Result;
    }
}