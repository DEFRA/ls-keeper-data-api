namespace KeeperData.Infrastructure.ApiClients.Sts;

public interface IStsTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}