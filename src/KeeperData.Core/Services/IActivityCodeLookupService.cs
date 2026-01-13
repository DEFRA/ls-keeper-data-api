namespace KeeperData.Core.Services;

public interface IActivityCodeLookupService { Task<(string? premiseType, string? premiseActivityType)> FindByActivityCodeAsync(string? activityCode, CancellationToken cancellationToken); }