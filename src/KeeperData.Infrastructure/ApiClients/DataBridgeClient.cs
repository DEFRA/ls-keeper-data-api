using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure.ApiClients.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace KeeperData.Infrastructure.ApiClients;

public class DataBridgeClient(
    IHttpClientFactory factory,
    IConfiguration configuration,
    ILogger<DataBridgeClient> logger) : IDataBridgeClient
{
    private readonly HttpClient _httpClient = factory.CreateClient(ClientName);
    private readonly ILogger<DataBridgeClient> _logger = logger;

    private readonly string? _serviceName = configuration.GetValue<string>("ApiClients:DataBridgeApi:ServiceName");
    private readonly bool _serviceNameExists = !string.IsNullOrWhiteSpace(configuration.GetValue<string>("ApiClients:DataBridgeApi:ServiceName"));

    private readonly bool _ctsAgentsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:CtsAgentsEnabled");
    private readonly bool _ctsKeepersEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:CtsKeepersEnabled");
    private readonly bool _ctsHoldingsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:CtsHoldingsEnabled");

    private readonly bool _samHoldingsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHoldingsEnabled");
    private readonly bool _samHoldersEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHoldersEnabled");
    private readonly bool _samHerdsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHerdsEnabled");
    private readonly bool _samPartiesEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamPartiesEnabled");

    private const string ClientName = "DataBridgeApi";

    private string GetUri(string path) => _serviceNameExists ? $"{_serviceName}/{path}" : path;

    public async Task<DataBridgeResponse<T>?> GetSamHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_samHoldingsEnabled) return null;

        var query = DataBridgeQueries.PagedRecords(top, skip, selectFields, updatedSinceDateTime);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHoldings, new { }, query);

        var result = await GetFromApiAsync<T>(
            uri,
            $"Sam paged holdings for top '{top}', skip '{skip}'",
            cancellationToken);

        return result;
    }

    public async Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        if (!_samHoldingsEnabled) return [];

        var query = DataBridgeQueries.SamHoldingsByCph(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHoldings, new { }, query);

        var result = await GetFromApiAsync<SamCphHolding>(
            uri,
            $"Sam holdings for ID '{id}'",
            cancellationToken);

        return result.Data;
    }

    public async Task<DataBridgeResponse<T>?> GetSamHoldersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_samHoldersEnabled) return null;

        var query = DataBridgeQueries.PagedRecords(top, skip, selectFields, updatedSinceDateTime);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHolders, new { }, query);

        var result = await GetFromApiAsync<T>(
            uri,
            $"Sam paged holders for top '{top}', skip '{skip}'",
            cancellationToken);

        return result;
    }

    public async Task<List<SamCphHolder>> GetSamHoldersByPartyIdAsync(string id, CancellationToken cancellationToken)
    {
        if (!_samHoldersEnabled) return [];

        var query = DataBridgeQueries.SamHolderByPartyId(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHolders, new { }, query);

        var result = await GetFromApiAsync<SamCphHolder>(
            uri,
            $"Sam holder for PARTY_ID '{id}'",
            cancellationToken);

        return result.Data;
    }

    public async Task<DataBridgeResponse<T>?> GetSamHerdsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_samHerdsEnabled) return null;

        var query = DataBridgeQueries.PagedRecords(top, skip, selectFields, updatedSinceDateTime);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHerds, new { }, query);

        var result = await GetFromApiAsync<T>(
            uri,
            $"Sam paged herds for top '{top}', skip '{skip}'",
            cancellationToken);

        return result;
    }

    public async Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        if (!_samHerdsEnabled) return [];

        var query = DataBridgeQueries.SamHerdsByCph(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHerds, new { }, query);

        var result = await GetFromApiAsync<SamHerd>(
            uri,
            $"Sam herds for ID '{id}'",
            cancellationToken);

        return result.Data;
    }

    public async Task<SamParty?> GetSamPartyAsync(string id, CancellationToken cancellationToken)
    {
        if (!_samPartiesEnabled) return null;

        var query = DataBridgeQueries.SamPartyByPartyId(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamParties, new { }, query);

        var result = await GetFromApiAsync<SamParty>(
            uri,
            $"Sam party for ID '{id}'",
            cancellationToken);

        return result.Data?.FirstOrDefault() ?? null;
    }

    public async Task<DataBridgeResponse<T>?> GetSamPartiesAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_samPartiesEnabled) return null;

        var query = DataBridgeQueries.PagedRecords(top, skip, selectFields, updatedSinceDateTime);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamParties, new { }, query);

        var result = await GetFromApiAsync<T>(
            uri,
            $"Sam paged parties for top '{top}', skip '{skip}'",
            cancellationToken);

        return result;
    }

    public async Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        if (!_samPartiesEnabled) return [];

        var query = DataBridgeQueries.SamPartiesByPartyIds(ids);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamParties, new { }, query);

        var result = await GetFromApiAsync<SamParty>(
            uri,
            $"Sam parties for IDs '{string.Join(",", ids)}'",
            cancellationToken);

        return result.Data;
    }

    public async Task<DataBridgeResponse<T>?> GetCtsHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_ctsHoldingsEnabled) return null;

        var query = DataBridgeQueries.PagedRecords(top, skip, selectFields, updatedSinceDateTime);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsHoldings, new { }, query);

        var result = await GetFromApiAsync<T>(
            uri,
            $"CTS paged holdings for top '{top}', skip '{skip}'",
            cancellationToken);

        return result;
    }

    public async Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        if (!_ctsHoldingsEnabled) return [];

        var query = DataBridgeQueries.CtsHoldingsByLidFullIdentifier(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsHoldings, new { }, query);

        var result = await GetFromApiAsync<CtsCphHolding>(
            uri,
            $"CTS holdings for ID '{id}'",
            cancellationToken);

        return result.Data;
    }

    public async Task<DataBridgeResponse<T>?> GetCtsAgentsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_ctsAgentsEnabled) return null;

        var query = DataBridgeQueries.PagedRecords(top, skip, selectFields, updatedSinceDateTime);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsAgents, new { }, query);

        var result = await GetFromApiAsync<T>(
            uri,
            $"CTS paged agents for top '{top}', skip '{skip}'",
            cancellationToken);

        return result;
    }

    public async Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        if (!_ctsAgentsEnabled) return [];

        var query = DataBridgeQueries.CtsAgentsByLidFullIdentifier(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsAgents, new { }, query);

        var result = await GetFromApiAsync<CtsAgentOrKeeper>(
            uri,
            $"CTS agents for ID '{id}'",
            cancellationToken);

        return result.Data;
    }

    public async Task<DataBridgeResponse<T>?> GetCtsKeepersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!_ctsKeepersEnabled) return null;

        var query = DataBridgeQueries.PagedRecords(top, skip, selectFields, updatedSinceDateTime);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsKeepers, new { }, query);

        var result = await GetFromApiAsync<T>(
            uri,
            $"CTS paged keepers for top '{top}', skip '{skip}'",
            cancellationToken);

        return result;
    }

    public async Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken)
    {
        if (!_ctsKeepersEnabled) return [];

        var query = DataBridgeQueries.CtsKeepersByLidFullIdentifier(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsKeepers, new { }, query);

        var result = await GetFromApiAsync<CtsAgentOrKeeper>(
            uri,
            $"CTS keepers for ID '{id}'",
            cancellationToken);

        return result.Data;
    }

    private async Task<DataBridgeResponse<T>> GetFromApiAsync<T>(string requestUri, string context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initiating API call: {context}, URI: {uri}", context, requestUri);

        try
        {
            using var response = await _httpClient.GetAsync(GetUri(requestUri), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogWarning("API call failed: {context}, Status: {status}, Response: {response}",
                    context, response.StatusCode, content);

                if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)
                {
                    throw new RetryableException(
                        $"Transient failure when calling {context}. " +
                        $"Status: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {content}");
                }

                throw new NonRetryableException(
                    $"Permanent failure when calling {context}. " +
                    $"Status: {(int)response.StatusCode} {response.ReasonPhrase}. Response: {content}");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<DataBridgeResponse<T>>(
                stream,
                JsonDefaults.DefaultOptionsWithDataBridgeApiSupport,
                cancellationToken);

            if (result == null)
            {
                _logger.LogError("Deserialization returned null: {context}", context);
                throw new NonRetryableException($"Deserialization returned null for {context}.");
            }

            _logger.LogInformation("API call succeeded: {context}", context);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network failure during API call: {context}", context);
            throw new RetryableException($"Network failure when calling {context}.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout during API call: {context}", context);
            throw new RetryableException($"Timeout when calling {context}.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Deserialization error: {context}", context);
            throw new NonRetryableException($"Deserialization error for {context}.", ex);
        }
    }
}