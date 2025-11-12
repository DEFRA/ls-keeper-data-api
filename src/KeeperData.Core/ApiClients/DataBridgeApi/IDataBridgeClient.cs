using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Core.ApiClients.DataBridgeApi;

public interface IDataBridgeClient
{
    Task<DataBridgeResponse<SamCphHolding>?> GetSamHoldingsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<SamCphHolder>?> GetSamHoldersAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamCphHolder>> GetSamHoldersByPartyIdAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<SamHerd>?> GetSamHerdsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken);

    Task<SamParty?> GetSamPartyAsync(string id, CancellationToken cancellationToken);
    Task<DataBridgeResponse<SamParty>?> GetSamPartiesAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken);

    Task<DataBridgeResponse<CtsCphHolding>?> GetCtsHoldingsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<CtsAgentOrKeeper>?> GetCtsAgentsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<CtsAgentOrKeeper>?> GetCtsKeepersAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken);
}