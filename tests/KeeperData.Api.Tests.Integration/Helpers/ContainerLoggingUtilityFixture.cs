namespace KeeperData.Api.Tests.Integration.Helpers;

using DotNet.Testcontainers.Containers;

public static class ContainerLoggingUtilityFixture
{
    public static async Task<bool> FindContainerLogEntryAsync(IContainer container, string entryToMatch)
    {
        var (stdout, stderr) = await container.GetLogsAsync();
        var logs = $"{stdout}\n{stderr}";
        return logs.Contains(entryToMatch);
    }

    public static async Task<List<string>> FindContainerLogEntriesAsync(IContainer container, string entryFragment)
    {
        var (stdout, stderr) = await container.GetLogsAsync();
        var logs = $"{stdout}\n{stderr}";

        var matchingLines = logs
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains(entryFragment))
            .ToList();

        return matchingLines;
    }
}