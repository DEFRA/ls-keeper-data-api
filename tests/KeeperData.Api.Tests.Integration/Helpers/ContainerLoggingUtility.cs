namespace KeeperData.Api.Tests.Integration.Helpers;

using DotNet.Testcontainers.Containers;

public static class ContainerLoggingUtility
{
    public static async Task<bool> FindContainerLogEntryAsync(IContainer container, string entryToMatch)
    {
        var (stdout, stderr) = await container.GetLogsAsync();
        var logs = $"{stdout}\n{stderr}";
        return logs.Contains(entryToMatch);
    }

    public static async Task<bool> WaitForContainerLogEntryAsync(
        IContainer container,
        string entryToMatch,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);
        var effectivePollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        var deadline = DateTime.UtcNow.Add(effectiveTimeout);

        while (true)
        {
            if (await FindContainerLogEntryAsync(container, entryToMatch))
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(effectivePollInterval, cancellationToken);
        }
    }

    public static async Task<List<string>> FindContainerLogEntriesAsync(IContainer container, string entryFragment)
    {
        var (stdout, stderr) = await container.GetLogsAsync();
        var logs = $"{stdout}\n{stderr}";

        var matchingLines = logs
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains(entryFragment))
            .ToList();

        return matchingLines;
    }
}