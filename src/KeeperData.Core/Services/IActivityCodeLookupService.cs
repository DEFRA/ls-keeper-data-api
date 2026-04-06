namespace KeeperData.Core.Services;

public interface IActivityCodeLookupService { Task<(string? siteType, string? siteActivityType)> FindByActivityCodeAsync(string? activityCode, CancellationToken cancellationToken); }