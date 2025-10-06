using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(1)]
public class CtsHoldingInsertedRawAggregationStep : ImportStepBase<CtsHoldingInsertedContext>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    private const string ClientName = "DataBridgeApi";

    public CtsHoldingInsertedRawAggregationStep(
        IHttpClientFactory httpClientFactory,
        ILogger<CtsHoldingInsertedRawAggregationStep> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient(ClientName);
    }

    protected override async Task ExecuteCoreAsync(CtsHoldingInsertedContext context, CancellationToken cancellationToken)
    {
        // Make API calls using _httpClient using Cph and BatchId

        // Construct Raw model
        context.RawHolding = new CtsCphHolding
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "I"
        };

        if (context.RawHolding.CHANGE_TYPE != DataBridgeConstants.ChangeTypeInsert)
        {
            await Task.CompletedTask;
        }

        context.RawAgents = [];

        context.RawKeepers = [];

        await Task.CompletedTask;
    }
}