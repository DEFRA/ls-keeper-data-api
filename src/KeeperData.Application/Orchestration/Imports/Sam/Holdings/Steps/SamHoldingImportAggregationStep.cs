using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;

[StepOrder(1)]
public class SamHoldingImportAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<SamHoldingImportAggregationStep> logger) : ImportStepBase<SamHoldingImportContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        var getHoldingsTask = _dataBridgeClient.GetSamHoldingsAsync(context.Cph, cancellationToken);
        var getHoldersTask = _dataBridgeClient.GetSamHoldersByCphAsync(context.Cph, cancellationToken);
        var getHerdsTask = _dataBridgeClient.GetSamHerdsAsync(context.Cph, cancellationToken);
        var getPortsTask = _dataBridgeClient.GetSamPortsAsync(context.Cph, cancellationToken);
        var getCommonLandsByCommonCphTask = _dataBridgeClient.GetSamCommonLandsByCommonCphAsync(context.Cph, cancellationToken);
        var getShowgroundsTask = _dataBridgeClient.GetSamShowgroundsByCphAsync(context.Cph, cancellationToken);

        await Task.WhenAll(
            getHoldingsTask,
            getHoldersTask,
            getHerdsTask,
            getPortsTask,
            getCommonLandsByCommonCphTask,
            getShowgroundsTask);

        context.RawHoldings = getHoldingsTask.Result;

        context.RawHerds = getHerdsTask.Result;

        context.RawHolders = getHoldersTask.Result;

        context.RawPorts = getPortsTask.Result;
        logger.LogInformation("Fetched {Count} raw port(s) for CPH {Cph}", context.RawPorts?.Count ?? 0, context.Cph);

        context.RawCommonLandsByCommonCph = getCommonLandsByCommonCphTask.Result;

        var parties = await GetSamPartiesAsync(context, cancellationToken);
        context.RawParties = SamPartyMapper.AggregatePartyAndHolder(parties, context.RawHolders);
        context.RawShowgrounds = getShowgroundsTask.Result;
    }

    private async Task<List<SamParty>> GetSamPartiesAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        var uniquePartyIds = (context.RawHerds ?? Enumerable.Empty<SamHerd>())
            .SelectMany(h => h.KeeperPartyIdList
                .Union(h.OwnerPartyIdList, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (uniquePartyIds.Count == 0)
            return [];

        return await _dataBridgeClient.GetSamPartiesAsync(uniquePartyIds, cancellationToken);
    }
}