using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily.Steps;

[StepOrder(1)]
public class CtsHoldingDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsHoldingDailyScanStep> logger) : ScanStepBase<CtsDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override Task ExecuteCoreAsync(CtsDailyScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
