using KeeperData.Application.Commands;
using KeeperData.Application.Queries;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;

namespace KeeperData.Application;

public interface IRequestExecutor
{
    Task<TResponse> ExecuteCommand<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    Task<TResponse> ExecuteCommand<TResponse>(ICommand<TrackedResult<TResponse>> command, CancellationToken cancellationToken = default);
    Task<TrackedResult<TResponse>> ExecuteTrackedCommand<TResponse>(ICommand<TrackedResult<TResponse>> command, CancellationToken cancellationToken = default);
    Task<TResponse> ExecuteQuery<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}