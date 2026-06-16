using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace KeeperData.Infrastructure.ApiClients.Fakes;

[ExcludeFromCodeCoverage]
public class FakeDataBridgeClient : IDataBridgeClient
{
    private readonly Random _random = new();

    public Task<DataBridgeResponse<T>?> GetSamHoldingsAsync<T>(
            int top,
            int skip,
            string? selectFields = null,
            DateTime? updatedSinceDateTime = null,
            string? orderBy = null,
            CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamCphHolding()).SelectMany(x => x).ToList();

        if (updatedSinceDateTime.HasValue)
        {
            data = data.Where(x => (x.UpdatedAtUtc >= updatedSinceDateTime) || (x.CreatedAtUtc >= updatedSinceDateTime)).ToList();
        }

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
            string? orderBy = null,
            CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamCphHoldersByCphOrPartyId()).SelectMany(x => x).ToList();

        if (updatedSinceDateTime.HasValue)
        {
            data = data.Where(x => (x.UpdatedAtUtc >= updatedSinceDateTime) || (x.CreatedAtUtc >= updatedSinceDateTime)).ToList();
        }

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
            string? orderBy = null,
            CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamHerd()).SelectMany(x => x).ToList();

        if (updatedSinceDateTime.HasValue)
        {
            data = data.Where(x => (x.UpdatedAtUtc >= updatedSinceDateTime) || (x.CreatedAtUtc >= updatedSinceDateTime)).ToList();
        }

        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamHerd(id));
    }

    public Task<DataBridgeResponse<T>?> GetSamHerdsByPartyIdAsync<T>(
        string partyId,
        string selectFields,
        string orderBy,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, 2).Select(_ => GetSamHerd()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, 0, 0);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
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
            string? orderBy = null,
            CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamParties()).SelectMany(x => x).ToList();

        if (updatedSinceDateTime.HasValue)
        {
            data = data.Where(x => (x.UpdatedAtUtc >= updatedSinceDateTime) || (x.CreatedAtUtc >= updatedSinceDateTime)).ToList();
        }

        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        return Task.FromResult(ids.Select(GetSamParty).ToList());
    }

    public Task<DataBridgeResponse<T>?> GetSamPortsAsync<T>(
            int top,
            int skip,
            string? selectFields = null,
            DateTime? updatedSinceDateTime = null,
            string? orderBy = null,
            CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamPort()).SelectMany(x => x).ToList();

        if (updatedSinceDateTime.HasValue)
        {
            data = data.Where(x => (x.UpdatedAtUtc >= updatedSinceDateTime) || (x.CreatedAtUtc >= updatedSinceDateTime)).ToList();
        }

        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamPort>> GetSamPortsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamPort(id));
    }

    public Task<DataBridgeResponse<T>?> GetCtsHoldingsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
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
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        return GenerateFakeCtsAgentOrKeeperResponseAsync<T>(top, skip);
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsAgentOrKeeper(id));
    }

    public Task<CtsAgentOrKeeper?> GetCtsAgentByPartyIdAsync(string partyId, CancellationToken cancellationToken)
    {
        var agents = GetCtsAgentOrKeeper();
        if (agents.Count > 0)
        {
            agents[0].PAR_ID = partyId;

            if (string.IsNullOrEmpty(agents[0].LID_FULL_IDENTIFIER))
            {
                agents[0].LID_FULL_IDENTIFIER = "AG-123456789";
            }
            return Task.FromResult<CtsAgentOrKeeper?>(agents[0]);
        }
        return Task.FromResult<CtsAgentOrKeeper?>(null);
    }

    public Task<DataBridgeResponse<T>?> GetCtsKeepersAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        return GenerateFakeCtsAgentOrKeeperResponseAsync<T>(top, skip);
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsAgentOrKeeper(id));
    }

    public Task<CtsAgentOrKeeper?> GetCtsKeeperByPartyIdAsync(string partyId, CancellationToken cancellationToken)
    {
        var keepers = GetCtsAgentOrKeeper();
        if (keepers.Count > 0)
        {
            keepers[0].PAR_ID = partyId;
            return Task.FromResult<CtsAgentOrKeeper?>(keepers[0]);
        }
        return Task.FromResult<CtsAgentOrKeeper?>(null);
    }

    public Task<DataBridgeResponse<T>?> GetSamCommonLandsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var data = GetSamCommonLands();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamCommonLand>> GetSamCommonLandsByCommonCphAsync(string cph, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCommonLands(commonCph: cph));
    }

    public Task<DataBridgeResponse<T>?> GetSamShowgroundsAsync<T>(
        int top,
        int skip,
        string? selectFields = null,
        DateTime? updatedSinceDateTime = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetSamShowground()).SelectMany(x => x).ToList();

        if (updatedSinceDateTime.HasValue)
        {
            data = data.Where(x => (x.UpdatedAtUtc >= updatedSinceDateTime) || (x.CreatedAtUtc >= updatedSinceDateTime)).ToList();
        }

        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }

    public Task<List<SamShowground>> GetSamShowgroundsByCphAsync(string cph, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamShowground(cph));
    }

    private Task<DataBridgeResponse<T>?> GenerateFakeCtsAgentOrKeeperResponseAsync<T>(int top, int skip)
    {
        var data = Enumerable.Range(0, top).Select(_ => GetCtsAgentOrKeeper()).SelectMany(x => x).ToList();
        var objects = JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(data));
        var response = GetDataBridgeResponse(objects!, top, skip);
        return Task.FromResult<DataBridgeResponse<T>?>(response);
    }
    private static DataBridgeResponse<T> GetDataBridgeResponse<T>(List<T> data, int top, int skip)
    {
        return new DataBridgeResponse<T>
        {
            CollectionName = "collection",
            Count = data.Count,

            TotalCount = data.Count + skip + 10, // fake more data existing

            Data = data,
            Top = top,
            Skip = skip
        };
    }

    private List<SamCphHolding> GetSamCphHolding(string? id = null)
    {
        return [
            new SamCphHolding
            {
                ANIMAL_PRODUCTION_USAGE_CODE = "MEAT",
                ANIMAL_SPECIES_CODE = "CTT",
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                COUNTRY_CODE = "GB",
                CPH = id ?? $"{_random.Next(10, 99)}/{_random.Next(100, 999):000}/{_random.Next(1000, 9999)}",
                CPH_RELATIONSHIP_TYPE = "MAIN",
                CPH_TYPE = "PERMANENT",
                CreatedAtUtc = DateTime.UtcNow,
                DISEASE_TYPE = null,
                EASTING = 400022,
                FACILITY_BUSINSS_ACTVTY_CODE = "FACACT",
                FACILITY_TYPE_CODE = "CL",
                FCLTY_SUB_BSNSS_ACTVTY_CODE = "FACSUB",
                FEATURE_ADDRESS_FROM_DATE = DateTime.Today.AddDays(-1),
                FEATURE_ADDRESS_TO_DATE = null,
                FEATURE_NAME = "Feature 22",
                INTERVAL = 12m,
                INTERVAL_UNIT_OF_TIME = "Months",
                IsDeleted = false,
                LOCALITY = "Locality22",
                MOVEMENT_RSTRCTN_RSN_CODE = null,
                NORTHING = 500022,
                OS_MAP_REFERENCE = null,
                PAON_END_NUMBER = 20,
                PAON_END_NUMBER_SUFFIX = 'D',
                PAON_START_NUMBER = 2,
                PAON_START_NUMBER_SUFFIX = 'C',
                POSTCODE = "CPH22 222",
                SAON_END_NUMBER = 10,
                SAON_END_NUMBER_SUFFIX = 'B',
                SAON_START_NUMBER = 1,
                SAON_START_NUMBER_SUFFIX = 'A',
                SECONDARY_CPH = "00/000/9267",
                STREET = "Holding Street 22",
                TOWN = "Town22",
                UDPRN = "25000022",
                UK_INTERNAL_CODE = "ENGLAND",
                UpdatedAtUtc = DateTime.UtcNow
            }];
    }

    private static List<SamCphHolder> GetSamCphHoldersByCphOrPartyId(string? partyId = null, string? holdingIdentifier = null)
    {
        return [
            new SamCphHolder
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
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
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
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

    private static List<SamParty> GetSamParties(string? id = null)
    {
        return [
            new SamParty
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                PARTY_ID = id ?? $"C{Guid.NewGuid().ToString("N")[..8]}",
                ORGANISATION_NAME = Guid.NewGuid().ToString(),
                PARTY_ROLE_FROM_DATE = DateTime.Today.AddDays(-1),
                ROLES = "AGENT"
            }];
    }

    private List<SamPort> GetSamPort(string? id = null)
    {
        return [
            new SamPort {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CPH = id ?? $"{_random.Next(10, 99)}/{_random.Next(100, 999)}/{_random.Next(1000, 9999)}",
                PREMISES_NAME = "Test Port",
                ADDRESS_LINE_1 = "Harbour Office",
                ADDRESS_LINE_2 = "Port Road",
                ADDRESS_LINE_3 = "Portstown",
                POSTCODE = "PT1 1PT",
                MAP_REFERENCE = $"AB{_random.Next(100000, 999999)}",
                EASTING = _random.Next(100000, 600000),
                NORTHING = _random.Next(100000, 600000)
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

    private static List<SamCommonLand> GetSamCommonLands(string? commonCph = null)
    {
        var cph = commonCph ?? "00/000/0001";
        return [
            new SamCommonLand
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                COMMON_CPH = cph,
                MAIN_CPH = "-",
                COMMON_LAND_PREMISE_ID = "546196",
                BUSINESS_USAGE = "Common Land",
                PREMISES_NAME = "-",
                ADDRESS_LINE_1 = "Land off Fawdon Park Road",
                LOCAL_AUTH_NAME = "TEST COUNCIL",
                COUNTRY = "England",
                EASTING = "422473",
                NORTHING = "569204",
                LINK_ID = "-1",
                CONTIGUOUS_COMMON = "No"
            }];
    }
    private List<SamShowground> GetSamShowground(string? id = null)
    {
        return [
            new SamShowground {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                CPH = id ?? $"{_random.Next(10, 99)}/{_random.Next(100, 999)}/{_random.Next(1000, 9999)}",
                START_DATE = DateTime.Today.AddDays(-10),
                END_DATE = null
            }];
    }
}