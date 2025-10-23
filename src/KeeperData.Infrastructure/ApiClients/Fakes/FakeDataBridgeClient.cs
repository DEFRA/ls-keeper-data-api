using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Infrastructure.ApiClients.Fakes;

public class FakeDataBridgeClient : IDataBridgeClient
{
    private readonly Random _random = new();

    public Task<List<SamCphHolding>> GetSamHoldingsAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamCphHolder>> GetSamHoldersAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamHerd>> GetSamHerdsAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SamParty> GetSamPartyAsync(string id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<SamParty>> GetSamPartiesAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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

    private List<CtsCphHolding> GetCtsCphHolding(string id)
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