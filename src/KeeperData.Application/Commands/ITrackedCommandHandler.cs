using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using MediatR;

namespace KeeperData.Application.Commands;

public interface ITrackedCommandHandler<in TCommand, TResult>
    : IRequestHandler<TCommand, TrackedResult<TResult>> where TCommand
    : ICommand<TrackedResult<TResult>>
{
}