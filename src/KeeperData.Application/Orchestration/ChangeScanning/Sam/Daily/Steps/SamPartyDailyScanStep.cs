using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;

[StepOrder(3)]
public class SamPartyDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<SamPartyDailyScanStep> logger)
    : DailyScanStepBase<SamScanPartyIdentifier>(
        dataBridgeClient,
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        configuration,
        logger)
{
    private const string SelectFields = "PARTY_ID";
    private const string OrderBy = "PARTY_ID asc";
    private const string HerdSelectFields = "CPHH";
    private const string HerdOrderBy = "CPHH asc";

    protected override bool IsEntityEnabled()
        => Configuration.GetValue<bool>("DataBridgeCollectionFlags:SamPartiesEnabled");

    protected override EntityScanContext GetScanContext(SamDailyScanContext context)
        => context.Parties;

    protected override async Task<DataBridgeResponse<SamScanPartyIdentifier>?> QueryDataAsync(
        SamDailyScanContext context,
        CancellationToken cancellationToken)
        => await DataBridgeClient.GetSamPartiesAsync<SamScanPartyIdentifier>(
            context.Parties.CurrentTop,
            context.Parties.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<SamScanPartyIdentifier> queryResponse,
        CancellationToken cancellationToken)
    {
        var identifiers = queryResponse.Data
            .Select(x => x.PARTY_ID)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        foreach (var id in identifiers)
        {
            var relatedHerdsResponse = await DataBridgeClient.GetSamHerdsByPartyIdAsync<SamScanHerdIdentifier>(
                id,
                HerdSelectFields,
                HerdOrderBy,
                cancellationToken);

            if (relatedHerdsResponse == null || relatedHerdsResponse.Data.Count == 0)
            {
                continue;
            }

            var herdIdentifiers = relatedHerdsResponse.Data
                .Select(x => x.CPHH.CphhToCph())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var hid in herdIdentifiers)
            {
                var message = new SamUpdateHoldingMessage
                {
                    Id = Guid.NewGuid(),
                    Identifier = hid
                };

                await IntakeMessagePublisher.PublishAsync(message, cancellationToken);
            }
        }
    }
}