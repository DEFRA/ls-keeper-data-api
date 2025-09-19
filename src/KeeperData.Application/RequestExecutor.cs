using KeeperData.Application.Commands;
using KeeperData.Application.Queries;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using MediatR;

namespace KeeperData.Application;

public class RequestExecutor(IMediator mediator) : IRequestExecutor
{
    private readonly IMediator _mediator = mediator;

    // Executes a command with no tracked result
    public async Task<TResponse> ExecuteCommand<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(command, cancellationToken);
    }

    // Executes a command and unwraps the tracked result
    public async Task<TResponse> ExecuteCommand<TResponse>(ICommand<TrackedResult<TResponse>> command, CancellationToken cancellationToken = default)
    {
        var tracked = await _mediator.Send(command, cancellationToken);
        return tracked.Result;
    }

    // Executes a command and returns the full tracked result
    public async Task<TrackedResult<TResponse>> ExecuteTrackedCommand<TResponse>(ICommand<TrackedResult<TResponse>> command, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(command, cancellationToken);
    }

    // Executes a query (assumed to return plain TResponse)
    public async Task<TResponse> ExecuteQuery<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(query, cancellationToken);
    }
}