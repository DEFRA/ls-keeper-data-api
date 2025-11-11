using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;

[StepOrder(2)]
public class SamHoldingDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    ILogger<SamHoldingDailyScanStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;

    protected override Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
