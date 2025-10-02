using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Steps;

[StepOrder(1)]
public class CtsRawAggregationStep : ImportStepBase<CtsHoldingImportContext>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    private const string ClientName = "DataBridgeApi";

    public CtsRawAggregationStep(
        IHttpClientFactory httpClientFactory,
        ILogger<CtsRawAggregationStep> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient(ClientName);
    }

    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        // Make API calls using _httpClient

        // Construct Raw model
        context.RawHolding = new CtsCphHolding
        {
        };

        context.RawAgents = [];

        context.RawKeepers = [];

        await Task.CompletedTask;
    }
}