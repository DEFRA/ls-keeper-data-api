using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Updates.Steps;

[StepOrder(1)]
public class SamHoldingUpdateAggregationStep : ImportStepBase<SamHoldingUpdateContext>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    private const string ClientName = "DataBridgeApi";

    public SamHoldingUpdateAggregationStep(
        IHttpClientFactory httpClientFactory,
        ILogger<SamHoldingUpdateAggregationStep> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient(ClientName);
    }

    protected override async Task ExecuteCoreAsync(SamHoldingUpdateContext context, CancellationToken cancellationToken)
    {
        // Make API calls using _httpClient using Cph and BatchId
        var samCphHolding = new SamCphHolding
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "U"
        };

        if (samCphHolding is not { CHANGE_TYPE: DataBridgeConstants.ChangeTypeUpdate })
            return;

        // Construct Raw model
        context.RawHolding = samCphHolding;

        await Task.CompletedTask;
    }
}