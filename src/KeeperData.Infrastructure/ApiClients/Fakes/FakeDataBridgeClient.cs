using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Infrastructure.ApiClients.Fakes;

public class FakeDataBridgeClient : IDataBridgeClient
{
    private readonly Random _random = new();

    public Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCphHolding(id));
    }

    public Task<List<SamCphHolder>> GetSamHoldersAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamCphHolder(id));
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamHerd(id));
    }

    public Task<SamParty> GetSamPartyAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetSamParty(id));
    }

    public Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        return Task.FromResult(ids.Select(GetSamParty).ToList());
    }

    public Task<List<CtsCphHolding>> GetCtsHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsCphHolding(id));
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsAgentsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsAgentOrKeeper(id));
    }

    public Task<List<CtsAgentOrKeeper>> GetCtsKeepersAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetCtsAgentOrKeeper(id));
    }

    private static List<SamCphHolding> GetSamCphHolding(string id)
    {
        return [
            new SamCphHolding {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                CPH = id,
                FEATURE_NAME = Guid.NewGuid().ToString(),
                CPH_TYPE = "PERMANENT",
                FEATURE_ADDRESS_FROM_DATE = DateTime.Today.AddDays(-1)
            }];
    }

    private List<SamCphHolder> GetSamCphHolder(string id)
    {
        return [
            new SamCphHolder {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                CPHS = string.Join(",", [id]),
                PARTY_ID = $"C{_random.Next(1, 9):D6}",
                ORGANISATION_NAME = Guid.NewGuid().ToString()
            }];
    }

    private List<SamHerd> GetSamHerd(string id)
    {
        return [
            new SamHerd {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                HERDMARK = Guid.NewGuid().ToString(),
                CPHH = id,
                ANIMAL_SPECIES_CODE = "CTT",
                ANIMAL_PURPOSE_CODE = "CTT-BEEF",
                KEEPER_PARTY_IDS = string.Join(",", [$"C{_random.Next(1, 9):D6}"]),
                OWNER_PARTY_IDS = string.Join(",", [$"C{_random.Next(1, 9):D6}"]),
                ANIMAL_GROUP_ID_MCH_FRM_DAT = DateTime.Today.AddDays(-1)
            }];
    }

    private static SamParty GetSamParty(string id)
    {
        return new SamParty
        {
            BATCH_ID = 1,
            CHANGE_TYPE = "I",
            IsDeleted = false,
            PARTY_ID = id,
            ORGANISATION_NAME = Guid.NewGuid().ToString(),
            PARTY_ROLE_FROM_DATE = DateTime.Today.AddDays(-1)
        };
    }

    private static List<CtsCphHolding> GetCtsCphHolding(string id)
    {
        return [
            new CtsCphHolding {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                LID_FULL_IDENTIFIER = id,
                ADR_NAME = Guid.NewGuid().ToString(),
                LOC_EFFECTIVE_FROM = DateTime.Today.AddDays(-1)
            }];
    }

    private List<CtsAgentOrKeeper> GetCtsAgentOrKeeper(string id)
    {
        return [
            new CtsAgentOrKeeper {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                IsDeleted = false,
                PAR_ID = _random.NextInt64(10000000000, 99999999999),
                LID_FULL_IDENTIFIER = id,
                PAR_SURNAME = Guid.NewGuid().ToString(),
                ADR_NAME = Guid.NewGuid().ToString(),
                LPR_EFFECTIVE_FROM_DATE = DateTime.Today.AddDays(-1)
            }];
    }
}