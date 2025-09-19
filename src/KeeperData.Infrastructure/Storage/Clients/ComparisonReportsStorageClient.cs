using KeeperData.Core.Storage;

namespace KeeperData.Infrastructure.Storage.Clients;

public class ComparisonReportsStorageClient : IStorageClient
{
    public string ClientName => GetType().Name;
}