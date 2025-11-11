using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;

[StepOrder(2)]
public class SamHoldingDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<SamHoldingDailyScanStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
