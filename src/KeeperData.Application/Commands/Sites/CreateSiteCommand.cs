using KeeperData.Core.Domain.BuildingBlocks.Aggregates;

namespace KeeperData.Application.Commands.Sites;

/// <summary>
/// Example implementation only. To remove in future stories.
/// </summary>
/// <param name="Name"></param>
public record CreateSiteCommand(string Name) : ICommand<TrackedResult<string>>;