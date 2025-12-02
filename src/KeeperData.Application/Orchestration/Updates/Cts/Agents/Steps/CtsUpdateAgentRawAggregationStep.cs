using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Agents.Steps;

[StepOrder(1)]
public class CtsUpdateAgentRawAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsUpdateAgentRawAggregationStep> logger) : UpdateStepBase<CtsUpdateAgentContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(CtsUpdateAgentContext context, CancellationToken cancellationToken)
    {
        context.RawAgent = await _dataBridgeClient.GetCtsAgentByPartyIdAsync(context.PartyId, cancellationToken);
    }
}