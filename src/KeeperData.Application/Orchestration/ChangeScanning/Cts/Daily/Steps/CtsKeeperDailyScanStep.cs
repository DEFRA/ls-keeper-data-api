using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily.Steps;

[StepOrder(2)]
public class CtsKeeperDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsKeeperDailyScanStep> logger) : ScanStepBase<CtsDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override Task ExecuteCoreAsync(CtsDailyScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
