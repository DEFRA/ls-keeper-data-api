using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Core.ApiClients.DataBridgeApi;

public interface IDataBridgeClient
{
    Task<DataBridgeResponse<T>?> GetSamHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<T>?> GetSamHoldersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamCphHolder>> GetSamHoldersByPartyIdAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<T>?> GetSamHerdsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken);

    Task<SamParty?> GetSamPartyAsync(string id, CancellationToken cancellationToken);
    Task<DataBridgeResponse<T>?> GetSamPartiesAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken);

    Task<DataBridgeResponse<T>?> GetCtsHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<T>?> GetCtsAgentsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken);

    Task<DataBridgeResponse<T>?> GetCtsKeepersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default);
    Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken);
    Task<CtsAgentOrKeeper?> GetCtsKeeperByPartyIdAsync(string partyId, CancellationToken cancellationToken);
}