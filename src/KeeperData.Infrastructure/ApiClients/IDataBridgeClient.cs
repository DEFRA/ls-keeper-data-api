using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Infrastructure.ApiClients;

public interface IDataBridgeClient
{
    Task<SamCphHolding> GetSamHoldingAsync(string id, int BatchId, CancellationToken cancellationToken);
    Task<List<SamCphHolder>> GetSamHoldersAsync(string id, int BatchId, CancellationToken cancellationToken);
    Task<List<SamParty>> GetSamPartiesAsync(string id, int BatchId, CancellationToken cancellationToken);
    Task<List<SamHerd>> GetSamHerdsAsync(string id, int BatchId, CancellationToken cancellationToken);

    Task<CtsCphHolding> GetCtsHoldingAsync(string id, int BatchId, CancellationToken cancellationToken);
    Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, int BatchId, CancellationToken cancellationToken);
    Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, int BatchId, CancellationToken cancellationToken);
}