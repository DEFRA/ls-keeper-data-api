using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Infrastructure.ApiClients;

public class DataBridgeClient(IHttpClientFactory factory) : IDataBridgeClient
{
    private readonly HttpClient _httpClient = factory.CreateClient(ClientName);

    private const string ClientName = "DataBridgeApi";

    public Task<SamCphHolding> GetSamHoldingAsync(string id, int BatchId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamCphHolder>> GetSamHoldersAsync(string id, int BatchId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamParty>> GetSamPartiesAsync(string id, int BatchId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, int BatchId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<CtsCphHolding> GetCtsHoldingAsync(string id, int BatchId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, int BatchId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, int BatchId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}