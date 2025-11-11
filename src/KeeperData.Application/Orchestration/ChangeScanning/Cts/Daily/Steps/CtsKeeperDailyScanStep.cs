using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily.Steps;

[StepOrder(2)]
public class CtsKeeperDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    ILogger<CtsKeeperDailyScanStep> logger) : ScanStepBase<CtsDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;

    protected override Task ExecuteCoreAsync(CtsDailyScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
