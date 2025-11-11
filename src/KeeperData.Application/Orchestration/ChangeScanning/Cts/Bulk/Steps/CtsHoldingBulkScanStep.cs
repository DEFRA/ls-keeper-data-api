using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk.Steps;

[StepOrder(1)]
public class CtsHoldingBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsHoldingBulkScanStep> logger) : ScanStepBase<CtsBulkScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override Task ExecuteCoreAsync(CtsBulkScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
