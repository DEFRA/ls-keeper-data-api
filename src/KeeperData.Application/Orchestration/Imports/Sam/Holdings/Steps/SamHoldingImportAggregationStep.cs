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
        var getHerdsTask = _dataBridgeClient.GetSamHerdsAsync(context.Cph, cancellationToken);

        await Task.WhenAll(
            getHoldingsTask,
            getHerdsTask);

        context.RawHoldings = getHoldingsTask.Result;

        context.RawHerds = getHerdsTask.Result;

        context.RawParties = await GetSamPartiesAsync(context, cancellationToken);
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