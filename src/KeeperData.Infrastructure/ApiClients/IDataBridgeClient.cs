using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Infrastructure.ApiClients;

public interface IDataBridgeClient
{
    Task<SamCphHolding> GetSamHoldingAsync(string id, CancellationToken cancellationToken);
    Task<List<SamCphHolder>> GetSamHoldersAsync(string id, CancellationToken cancellationToken);
    Task<List<SamParty>> GetSamPartiesAsync(string id, CancellationToken cancellationToken);
    Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken);

    Task<CtsCphHolding> GetCtsHoldingAsync(string id, CancellationToken cancellationToken);
    Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken);
    Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken);
}