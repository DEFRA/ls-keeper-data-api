using KeeperData.Core.Storage;

namespace KeeperData.Infrastructure.Storage.Clients;

public class CphSqliteStorageClient : IStorageClient
{
    public string ClientName => GetType().Name;
}
