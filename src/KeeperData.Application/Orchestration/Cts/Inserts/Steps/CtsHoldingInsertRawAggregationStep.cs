using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Steps;

[StepOrder(1)]
public class CtsHoldingInsertRawAggregationStep : ImportStepBase<CtsHoldingInsertContext>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    private const string ClientName = "DataBridgeApi";

    public CtsHoldingInsertRawAggregationStep(
        IHttpClientFactory httpClientFactory,
        ILogger<CtsHoldingInsertRawAggregationStep> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient(ClientName);
    }

    protected override async Task ExecuteCoreAsync(CtsHoldingInsertContext context, CancellationToken cancellationToken)
    {
        // Make API calls using _httpClient using Cph and BatchId
        var ctsCphHolding = new CtsCphHolding
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "I"
        };

        if (ctsCphHolding is not { CHANGE_TYPE: DataBridgeConstants.ChangeTypeInsert })
            return;

        // Construct Raw model
        context.RawHolding = ctsCphHolding;

        context.RawAgents = [];

        context.RawKeepers = [];

        await Task.CompletedTask;
    }
}