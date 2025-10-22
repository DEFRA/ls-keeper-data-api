using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Inserts.Steps;

[StepOrder(1)]
public class SamHoldingInsertAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<SamHoldingInsertAggregationStep> logger) : ImportStepBase<SamHoldingInsertContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(SamHoldingInsertContext context, CancellationToken cancellationToken)
    {
        var getHoldingsTask = _dataBridgeClient.GetSamHoldingsAsync(context.Cph, cancellationToken);
        var getHoldersTask = _dataBridgeClient.GetSamHoldersAsync(context.Cph, cancellationToken);
        var getHerdsTask = _dataBridgeClient.GetSamHerdsAsync(context.Cph, cancellationToken);

        await Task.WhenAll(
            getHoldingsTask,
            getHoldersTask,
            getHerdsTask);

        context.RawHoldings = getHoldingsTask.Result;

        context.RawHolders = getHoldersTask.Result;

        context.RawHerds = getHerdsTask.Result;

        context.RawParties = await GetSamPartiesAsync(context, cancellationToken);

        await Task.CompletedTask;
    }

    private async Task<List<SamParty>> GetSamPartiesAsync(SamHoldingInsertContext context, CancellationToken cancellationToken)
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