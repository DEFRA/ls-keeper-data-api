using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using System.Text.Json;

namespace KeeperData.Infrastructure.ApiClients.Fakes;

public class FakeDataBridgeClient : IDataBridgeClient
{
    private readonly Random _random = new();

    public Task<DataBridgeResponse<T>?> GetSamHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamCphHolding()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCphHolding(id));
    }

    public Task<DataBridgeResponse<T>?> GetSamHoldersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamCphHoldersByCphOrPartyId()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamCphHolder>> GetSamHoldersByCphAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCphHoldersByCphOrPartyId(holdingIdentifier: id));
    }

    public Task<List<SamCphHolder>> GetSamHoldersByPartyIdAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCphHoldersByCphOrPartyId(partyId: id));
    }

    public Task<DataBridgeResponse<T>?> GetSamHerdsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamHerd()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamHerd(id));
    }

    public Task<SamParty?> GetSamPartyAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult<SamParty?>(GetSamParty(id));
    }

    public Task<DataBridgeResponse<T>?> GetSamPartiesAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamParties()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        return Task.FromResult(ids.Select(GetSamParty).ToList());
    }

    public Task<DataBridgeResponse<T>?> GetCtsHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetCtsCphHolding()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsCphHolding(id));
    }

    public Task<DataBridgeResponse<T>?> GetCtsAgentsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetCtsAgentOrKeeper()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsAgentOrKeeper(id));
    }

    public Task<DataBridgeResponse<T>?> GetCtsKeepersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetCtsAgentOrKeeper()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsAgentOrKeeper(id));
    }

    private static DataBridgeResponse<T> GetDataBridgeResponse<T>(List<T> data, int top, int skip)
    {
        return new DataBridgeResponse<T>
        {
            CollectionName = "collection",
            Count = data.Count,
            Data = data,
            Top = top,
            Skip = skip
        };
    }

    private List<SamCphHolding> GetSamCphHolding(string? id = null)
    {
        return [
            new SamCphHolding {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                CPH = id ?? $"{_random.Next(10, 99)}{_random.Next(100, 999)}{_random.Next(1000, 9999)}",
                FEATURE_NAME = Guid.NewGuid().ToString(),
                CPH_TYPE = "PERMANENT",
                FEATURE_ADDRESS_FROM_DATE = DateTime.Today.AddDays(-1)
            }];
    }

    private List<SamCphHolder> GetSamCphHoldersByCphOrPartyId(string? partyId = null, string? holdingIdentifier = null)
    {
        return [
            new SamCphHolder
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                CPHS = string.Join(",", [holdingIdentifier ?? Guid.NewGuid().ToString()]),
                PARTY_ID = partyId ?? $"C{Guid.NewGuid().ToString("N")[..8]}",
                ORGANISATION_NAME = Guid.NewGuid().ToString()
            }];
    }

    private List<SamHerd> GetSamHerd(string? id = null)
    {
        return [
            new SamHerd {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                HERDMARK = Guid.NewGuid().ToString(),
                CPHH = !string.IsNullOrWhiteSpace(id) ? $"{id}/01" : $"{_random.Next(10, 99)}{_random.Next(100, 999)}{_random.Next(1000, 9999)}/01",
                ANIMAL_SPECIES_CODE = "CTT",
                ANIMAL_PURPOSE_CODE = "CTT-BEEF",
                KEEPER_PARTY_IDS = string.Join(",", [$"C{Guid.NewGuid().ToString("N")[..8]}"]),
                OWNER_PARTY_IDS = string.Join(",", [$"C{Guid.NewGuid().ToString("N")[..8]}"]),
                ANIMAL_GROUP_ID_MCH_FRM_DAT = DateTime.Today.AddDays(-1)
            }];
    }

    private SamParty GetSamParty(string? id = null)
    {
        return new SamParty
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "I",
            IsDeleted = false,
            PARTY_ID = id ?? $"C{Guid.NewGuid().ToString("N")[..8]}",
            ORGANISATION_NAME = Guid.NewGuid().ToString(),
            PARTY_ROLE_FROM_DATE = DateTime.Today.AddDays(-1),
            ROLES = "AGENT"
        };
    }

    private List<SamParty> GetSamParties(string? id = null)
    {
        return [
            new SamParty
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                PARTY_ID = id ?? $"C{Guid.NewGuid().ToString("N")[..8]}",
                ORGANISATION_NAME = Guid.NewGuid().ToString(),
                PARTY_ROLE_FROM_DATE = DateTime.Today.AddDays(-1),
                ROLES = "AGENT"
            }];
    }

    private List<CtsCphHolding> GetCtsCphHolding(string? id = null)
    {
        return [
            new CtsCphHolding {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                LID_FULL_IDENTIFIER = id ?? $"AH-{_random.Next(10, 99)}{_random.Next(100, 999)}{_random.Next(1000, 9999)}",
                ADR_NAME = Guid.NewGuid().ToString(),
                LOC_EFFECTIVE_FROM = DateTime.Today.AddDays(-1)
            }];
    }

    private List<CtsAgentOrKeeper> GetCtsAgentOrKeeper(string? id = null)
    {
        return [
            new CtsAgentOrKeeper {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                PAR_ID = _random.NextInt64(10000000000, 99999999999).ToString(),
                LID_FULL_IDENTIFIER = id ?? $"AH-{_random.Next(10, 99)}{_random.Next(100, 999)}{_random.Next(1000, 9999)}",
                PAR_SURNAME = Guid.NewGuid().ToString(),
                ADR_NAME = Guid.NewGuid().ToString(),
                LPR_EFFECTIVE_FROM_DATE = DateTime.Today.AddDays(-1)
            }];
    }
}