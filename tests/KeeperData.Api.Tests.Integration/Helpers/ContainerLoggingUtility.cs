namespace KeeperData.Api.Tests.Integration.Helpers;

using DotNet.Testcontainers.Containers;

public static class ContainerLoggingUtility
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(1);

    public static async Task<bool> FindContainerLogEntryAsync(
        IContainer container,
        string entryToMatch,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetLogsAsync(container, cancellationToken);
        return logs.Contains(entryToMatch);
    }

    /// <summary>
    /// Polls the container logs until the entry is found or the timeout elapses.
    /// Guaranteed to return within the timeout, even if the container log fetch stalls.
    /// </summary>
    public static async Task<bool> WaitForContainerLogEntryAsync(
        IContainer container,
        string entryToMatch,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? DefaultTimeout;
        var effectivePollInterval = pollInterval ?? DefaultPollInterval;

        using var timeoutSource = new CancellationTokenSource(effectiveTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutSource.Token, cancellationToken);

        var token = linkedSource.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (await FindContainerLogEntryAsync(container, entryToMatch, token))
                {
                    return true;
                }

                await Task.Delay(effectivePollInterval, token);
            }
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            // Timed out, treated as not found so the assertion reports the real failure.
        }

        return false;
    }

    public static async Task<List<string>> FindContainerLogEntriesAsync(
        IContainer container,
        string entryFragment,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetLogsAsync(container, cancellationToken);

        return logs
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains(entryFragment))
            .ToList();
    }

    private static async Task<string> GetLogsAsync(IContainer container, CancellationToken cancellationToken)
    {
        var (stdout, stderr) = await container.GetLogsAsync(ct: cancellationToken);
        return $"{stdout}\n{stderr}";
    }
}