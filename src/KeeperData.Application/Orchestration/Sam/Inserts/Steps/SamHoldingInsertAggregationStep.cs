using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Inserts.Steps;

[StepOrder(1)]
public class SamHoldingInsertAggregationStep : ImportStepBase<SamHoldingInsertContext>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    private const string ClientName = "DataBridgeApi";

    public SamHoldingInsertAggregationStep(
        IHttpClientFactory httpClientFactory,
        ILogger<SamHoldingInsertAggregationStep> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient(ClientName);
    }

    protected override async Task ExecuteCoreAsync(SamHoldingInsertContext context, CancellationToken cancellationToken)
    {
        // Make API calls using _httpClient using Cph and BatchId

        // Construct Raw model
        context.RawHolding = new SamCphHolding
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "I"
        };

        if (context.RawHolding.CHANGE_TYPE != DataBridgeConstants.ChangeTypeInsert)
        {
            await Task.CompletedTask;
        }
            
        context.RawHolders = [];

        context.RawHerds = [];

        context.RawParties = [];

        await Task.CompletedTask;
    }
}