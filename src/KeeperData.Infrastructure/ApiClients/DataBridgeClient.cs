using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure.ApiClients.Extensions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace KeeperData.Infrastructure.ApiClients;

public class DataBridgeClient(IHttpClientFactory factory, ILogger<DataBridgeClient> logger) : IDataBridgeClient
{
    private readonly HttpClient _httpClient = factory.CreateClient(ClientName);
    private readonly ILogger<DataBridgeClient> _logger = logger;

    private const string ClientName = "DataBridgeApi";

    public async Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.SamHoldingsByCph(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHoldings, new { }, query);

        return await GetFromApiAsync<List<SamCphHolding>>(
            uri,
            $"Sam holdings for ID '{id}'",
            cancellationToken);
    }

    public async Task<List<SamCphHolder>> GetSamHoldersByPartyIdAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.SamHolderByPartyId(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHolders, new { }, query);

        return await GetFromApiAsync<List<SamCphHolder>>(
            uri,
            $"Sam holder for PARTY_ID '{id}'",
            cancellationToken);
    }

    public async Task<List<SamCphHolder>> GetSamHoldersByCphAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.SamHoldersByCph(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHolders, new { }, query);

        return await GetFromApiAsync<List<SamCphHolder>>(
            uri,
            $"Sam holders for ID '{id}'",
            cancellationToken);
    }

    public async Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.SamHerdsByCph(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamHerds, new { }, query);

        return await GetFromApiAsync<List<SamHerd>>(
            uri,
            $"Sam herds for ID '{id}'",
            cancellationToken);
    }

    public async Task<SamParty> GetSamPartyAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.SamPartyByPartyId(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamParties, new { }, query);

        return await GetFromApiAsync<SamParty>(
            uri,
            $"Sam party for ID '{id}'",
            cancellationToken);
    }

    public async Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.SamPartiesByPartyIds(ids);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetSamParties, new { }, query);

        return await GetFromApiAsync<List<SamParty>>(
            uri,
            $"Sam parties for IDs '{string.Join(",", ids)}'",
            cancellationToken);
    }

    public async Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.CtsHoldingsByLidFullIdentifier(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsHoldings, new { }, query);

        return await GetFromApiAsync<List<CtsCphHolding>>(
            uri,
            $"CTS holdings for ID '{id}'",
            cancellationToken);
    }

    public async Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.CtsAgentsByLidFullIdentifier(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsAgents, new { }, query);

        return await GetFromApiAsync<List<CtsAgentOrKeeper>>(
            uri,
            $"CTS agents for ID '{id}'",
            cancellationToken);
    }

    public async Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken)
    {
        var query = DataBridgeQueries.CtsKeepersByLidFullIdentifier(id);
        var uri = UriTemplate.Resolve(DataBridgeApiRoutes.GetCtsKeepers, new { }, query);

        return await GetFromApiAsync<List<CtsAgentOrKeeper>>(
            uri,
            $"CTS keepers for ID '{id}'",
            cancellationToken);
    }

    private async Task<T> GetFromApiAsync<T>(string requestUri, string context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initiating API call: {context}, URI: {uri}", context, requestUri);

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

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
            var result = await JsonSerializer.DeserializeAsync<T>(
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