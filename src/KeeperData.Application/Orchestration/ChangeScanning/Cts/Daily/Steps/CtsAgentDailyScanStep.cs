using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily.Steps;

[StepOrder(3)]
public class CtsAgentDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsAgentDailyScanStep> logger) : ScanStepBase<CtsDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override Task ExecuteCoreAsync(CtsDailyScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
