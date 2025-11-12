using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Infrastructure.ApiClients.Fakes;

public class FakeDataBridgeClient : IDataBridgeClient
{
    private readonly Random _random = new();

    public Task<DataBridgeResponse<SamCphHolding>?> GetSamHoldingsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamCphHolding()).SelectMany(x => x).ToList();
        var response = GetDataBridgeResponse(data, top, skip);
        return Task.FromResult<DataBridgeResponse<SamCphHolding>?>(response);
    }

    public Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCphHolding(id));
    }

    public Task<DataBridgeResponse<SamCphHolder>?> GetSamHoldersAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamCphHoldersByPartyId()).SelectMany(x => x).ToList();
        var response = GetDataBridgeResponse(data, top, skip);
        return Task.FromResult<DataBridgeResponse<SamCphHolder>?>(response);
    }

    public Task<List<SamCphHolder>> GetSamHoldersByPartyIdAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCphHoldersByPartyId(id));
    }

    public Task<DataBridgeResponse<SamHerd>?> GetSamHerdsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamHerd()).SelectMany(x => x).ToList();
        var response = GetDataBridgeResponse(data, top, skip);
        return Task.FromResult<DataBridgeResponse<SamHerd>?>(response);
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamHerd(id));
    }

    public Task<SamParty?> GetSamPartyAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult<SamParty?>(GetSamParty(id));
    }

    public Task<DataBridgeResponse<SamParty>?> GetSamPartiesAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamParties()).SelectMany(x => x).ToList();
        var response = GetDataBridgeResponse(data, top, skip);
        return Task.FromResult<DataBridgeResponse<SamParty>?>(response);
    }

    public Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        return Task.FromResult(ids.Select(GetSamParty).ToList());
    }

    public Task<DataBridgeResponse<CtsCphHolding>?> GetCtsHoldingsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetCtsCphHolding()).SelectMany(x => x).ToList();
        var response = GetDataBridgeResponse(data, top, skip);
        return Task.FromResult<DataBridgeResponse<CtsCphHolding>?>(response);
    }

    public Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsCphHolding(id));
    }

    public Task<DataBridgeResponse<CtsAgentOrKeeper>?> GetCtsAgentsAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetCtsAgentOrKeeper()).SelectMany(x => x).ToList();
        var response = GetDataBridgeResponse(data, top, skip);
        return Task.FromResult<DataBridgeResponse<CtsAgentOrKeeper>?>(response);
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsAgentOrKeeper(id));
    }

    public Task<DataBridgeResponse<CtsAgentOrKeeper>?> GetCtsKeepersAsync(
        int top,
        int skip,
        DateTime? updatedSinceDateTime = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetCtsAgentOrKeeper()).SelectMany(x => x).ToList();
        var response = GetDataBridgeResponse(data, top, skip);
        return Task.FromResult<DataBridgeResponse<CtsAgentOrKeeper>?>(response);
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

    private List<SamCphHolder> GetSamCphHoldersByPartyId(string? partyId = null, string? holdingIdentifier = null)
    {
        return [
            new SamCphHolder
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                CPHS = string.Join(",", [holdingIdentifier ?? "XX/XXX/XXXX"]),
                PARTY_ID = partyId ?? $"C{_random.Next(1, 9):D6}",
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
                CPHH = id ?? $"{_random.Next(10, 99)}{_random.Next(100, 999)}{_random.Next(1000, 9999)}/01",
                ANIMAL_SPECIES_CODE = "CTT",
                ANIMAL_PURPOSE_CODE = "CTT-BEEF",
                KEEPER_PARTY_IDS = string.Join(",", [$"C{_random.Next(1, 9):D6}"]),
                OWNER_PARTY_IDS = string.Join(",", [$"C{_random.Next(1, 9):D6}"]),
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
            PARTY_ID = id ?? $"C{_random.Next(1, 9):D6}",
            ORGANISATION_NAME = Guid.NewGuid().ToString(),
            PARTY_ROLE_FROM_DATE = DateTime.Today.AddDays(-1),
            ROLES = "Agent"
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
                PARTY_ID = id ?? $"C{_random.Next(1, 9):D6}",
                ORGANISATION_NAME = Guid.NewGuid().ToString(),
                PARTY_ROLE_FROM_DATE = DateTime.Today.AddDays(-1),
                ROLES = "Agent"
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