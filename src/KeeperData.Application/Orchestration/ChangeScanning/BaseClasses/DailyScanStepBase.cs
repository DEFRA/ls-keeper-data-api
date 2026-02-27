using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.BaseClasses;

public abstract class DailyScanStepBase<TContext, TIdentifier>(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger logger) : PaginatedScanStepBase<TContext, TIdentifier>(
    intakeMessagePublisher,
    dataBridgeScanConfiguration,
    delayProvider,
    logger)
    where TContext : ScanContext
{
    protected readonly IDataBridgeClient DataBridgeClient = dataBridgeClient;
    protected readonly IConfiguration Configuration = configuration;
}