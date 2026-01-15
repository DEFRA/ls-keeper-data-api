using KeeperData.Application.Commands;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using MediatR;

namespace KeeperData.Infrastructure.Behaviors;

public class DomainEventDispatchingBehavior<TRequest, TResponse>(IAggregateTracker tracker, IMediator mediator) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IAggregateTracker _tracker = tracker;
    private readonly IMediator _mediator = mediator;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        var aggregates = _tracker.GetTrackedAggregates();
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }

            aggregate.ClearDomainEvents();
        }

        _tracker.Clear();
        return response;
    }
}