using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Infrastructure.ApiClients;

public class DataBridgeClient(IHttpClientFactory factory) : IDataBridgeClient
{
    private readonly HttpClient _httpClient = factory.CreateClient(ClientName);

    private const string ClientName = "DataBridgeApi";

    public Task<SamCphHolding> GetSamHoldingAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamCphHolder>> GetSamHoldersAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamParty>> GetSamPartiesAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<CtsCphHolding> GetCtsHoldingAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}