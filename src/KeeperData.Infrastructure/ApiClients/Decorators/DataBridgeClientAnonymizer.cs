using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Infrastructure.Anonymization;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.ApiClients.Decorators;

public class DataBridgeClientAnonymizer(
    IDataBridgeClient inner,
    ILogger<DataBridgeClientAnonymizer> logger) : IDataBridgeClient
{

    public async Task<DataBridgeResponse<T>?> GetSamHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetSamHoldingsAsync<T>(top, skip, selectFields, updatedSinceDateTime, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result, logger);
        return result;
    }

    public async Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        var result = await inner.GetSamHoldingsAsync(id, cancellationToken);
        PiiAnonymizerHelper.AnonymizeAll(result, PiiAnonymizerHelper.AnonymizeSamHolding);
        return result;
    }

    public async Task<DataBridgeResponse<T>?> GetSamHoldersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetSamHoldersAsync<T>(top, skip, selectFields, updatedSinceDateTime, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result);
        return result;
    }

    public async Task<List<SamCphHolder>> GetSamHoldersByCphAsync(string id, CancellationToken cancellationToken)
    {
        var result = await inner.GetSamHoldersByCphAsync(id, cancellationToken);
        PiiAnonymizerHelper.AnonymizeAll(result, PiiAnonymizerHelper.AnonymizeSamHolder);
        return result;
    }

    public async Task<List<SamCphHolder>> GetSamHoldersByPartyIdAsync(string id, CancellationToken cancellationToken)
    {
        var result = await inner.GetSamHoldersByPartyIdAsync(id, cancellationToken);
        PiiAnonymizerHelper.AnonymizeAll(result, PiiAnonymizerHelper.AnonymizeSamHolder);
        return result;
    }

    public async Task<DataBridgeResponse<T>?> GetSamHerdsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetSamHerdsAsync<T>(top, skip, selectFields, updatedSinceDateTime, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result);
        return result;
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
        => inner.GetSamHerdsAsync(id, cancellationToken);

    public async Task<DataBridgeResponse<T>?> GetSamHerdsByPartyIdAsync<T>(
        string partyId,
        string selectFields,
        string orderBy,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetSamHerdsByPartyIdAsync<T>(partyId, selectFields, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result);
        return result;
    }

    public async Task<SamParty?> GetSamPartyAsync(string id, CancellationToken cancellationToken)
    {
        var result = await inner.GetSamPartyAsync(id, cancellationToken);
        if (result is not null)
            PiiAnonymizerHelper.AnonymizeSamParty(result);
        return result;
    }

    public async Task<DataBridgeResponse<T>?> GetSamPartiesAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetSamPartiesAsync<T>(top, skip, selectFields, updatedSinceDateTime, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result);
        return result;
    }

    public async Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var result = await inner.GetSamPartiesAsync(ids, cancellationToken);
        PiiAnonymizerHelper.AnonymizeAll(result, PiiAnonymizerHelper.AnonymizeSamParty);
        return result;
    }

    public async Task<DataBridgeResponse<T>?> GetCtsHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetCtsHoldingsAsync<T>(top, skip, selectFields, updatedSinceDateTime, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result);
        return result;
    }

    public async Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        var result = await inner.GetCtsHoldingsAsync(id, cancellationToken);
        PiiAnonymizerHelper.AnonymizeAll(result, PiiAnonymizerHelper.AnonymizeCtsHolding);
        return result;
    }

    public async Task<DataBridgeResponse<T>?> GetCtsAgentsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetCtsAgentsAsync<T>(top, skip, selectFields, updatedSinceDateTime, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result);
        return result;
    }

    public async Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        var result = await inner.GetCtsAgentsAsync(id, cancellationToken);
        PiiAnonymizerHelper.AnonymizeAll(result, PiiAnonymizerHelper.AnonymizeCtsAgentOrKeeper);
        return result;
    }

    public async Task<CtsAgentOrKeeper?> GetCtsAgentByPartyIdAsync(string partyId, CancellationToken cancellationToken)
    {
        var result = await inner.GetCtsAgentByPartyIdAsync(partyId, cancellationToken);
        if (result is not null)
            PiiAnonymizerHelper.AnonymizeCtsAgentOrKeeper(result);
        return result;
    }

    public async Task<DataBridgeResponse<T>?> GetCtsKeepersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetCtsKeepersAsync<T>(top, skip, selectFields, updatedSinceDateTime, orderBy, cancellationToken);
        PiiAnonymizerHelper.AnonymizeResponse(result);
        return result;
    }

    public async Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken)
    {
        var result = await inner.GetCtsKeepersAsync(id, cancellationToken);
        PiiAnonymizerHelper.AnonymizeAll(result, PiiAnonymizerHelper.AnonymizeCtsAgentOrKeeper);
        return result;
    }

    public async Task<CtsAgentOrKeeper?> GetCtsKeeperByPartyIdAsync(string partyId, CancellationToken cancellationToken)
    {
        var result = await inner.GetCtsKeeperByPartyIdAsync(partyId, cancellationToken);
        if (result is not null)
            PiiAnonymizerHelper.AnonymizeCtsAgentOrKeeper(result);
        return result;
    }
}