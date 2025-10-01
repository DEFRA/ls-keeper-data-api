using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Steps;

public class SamRawAggregationStep : ImportStepBase<SamHoldingImportContext>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    private const string ClientName = "DataBridgeApi";

    public SamRawAggregationStep(
        IHttpClientFactory httpClientFactory,
        ILogger<SamRawAggregationStep> logger)
        : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient(ClientName);
    }

    protected override async Task ExecuteCoreAsync(SamHoldingImportContext context, CancellationToken cancellationToken)
    {
        // Make API calls using _httpClient

        // Construct Raw model
        context.Raw = new SamCphHolding
        {
        };

        await Task.CompletedTask;
    }
}
