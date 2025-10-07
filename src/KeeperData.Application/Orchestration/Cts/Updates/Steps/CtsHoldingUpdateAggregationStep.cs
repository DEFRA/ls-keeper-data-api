using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Updates.Steps;

[StepOrder(1)]
public class CtsHoldingUpdateAggregationStep : ImportStepBase<CtsHoldingUpdateContext>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    private const string ClientName = "DataBridgeApi";

    public CtsHoldingUpdateAggregationStep(
        IHttpClientFactory httpClientFactory,
        ILogger<CtsHoldingUpdateAggregationStep> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient(ClientName);
    }

    protected override async Task ExecuteCoreAsync(CtsHoldingUpdateContext context, CancellationToken cancellationToken)
    {
        // Make API calls using _httpClient using Cph and BatchId
        var ctsCphHolding = new CtsCphHolding
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "U"
        };

        if (ctsCphHolding is not { CHANGE_TYPE: DataBridgeConstants.ChangeTypeUpdate })
            return;

        // Construct Raw model
        context.RawHolding = ctsCphHolding;

        await Task.CompletedTask;
    }
}