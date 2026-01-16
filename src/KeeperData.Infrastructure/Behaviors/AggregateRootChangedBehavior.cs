using KeeperData.Application.Commands;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using MediatR;
using System.Reflection;

namespace KeeperData.Infrastructure.Behaviors;

public class AggregateRootChangedBehavior<TRequest, TResponse>(IAggregateTracker tracker) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IAggregateTracker _tracker = tracker;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        TrackAggregates(response);

        return response;
    }

    private void TrackAggregates(object? result)
    {
        if (result is null) return;

        // Prefer TrackedResult<T>
        if (result is ITrackedResult tracked)
        {
            foreach (var aggregate in tracked.Aggregates)
            {
                _tracker.Track(aggregate);
            }
            return;
        }

        // Direct aggregate
        if (result is IAggregateRoot directAggregate)
        {
            _tracker.Track(directAggregate);
            return;
        }

        // Collection of aggregates
        if (result is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is IAggregateRoot agg)
                    _tracker.Track(agg);
            }
            return;
        }

        // Scan public properties for aggregates
        var props = result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            var value = prop.GetValue(result);
            if (value is IAggregateRoot propAggregate)
            {
                _tracker.Track(propAggregate);
            }
            else if (value is System.Collections.IEnumerable nestedEnumerable)
            {
                foreach (var item in nestedEnumerable)
                {
                    if (item is IAggregateRoot nestedAgg)
                        _tracker.Track(nestedAgg);
                }
            }
        }
    }
}