using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk.Steps;

[StepOrder(1)]
public class SamHolderBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<SamHolderBulkScanStep> logger) : ScanStepBase<SamBulkScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override Task ExecuteCoreAsync(SamBulkScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
