using KeeperData.Application.Commands;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using MediatR;
using System.Collections;
using System.Reflection;

namespace KeeperData.Infrastructure.Behaviors;

public class AggregateRootChangedBehavior<TRequest, TResponse>(IAggregateTracker tracker) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        TrackAggregates(response);

        return response;
    }

    private void TrackAggregates(object? result)
    {
        switch (result)
        {
            case null:
                return;
            case ITrackedResult tracked:
                TrackFromTrackedResult(tracked);
                return;
            case IAggregateRoot directAggregate:
                TrackDirectAggregate(directAggregate);
                return;
            case IEnumerable enumerable:
                TrackFromEnumerable(enumerable);
                return;
            default:
                TrackFromPublicProperties(result);
                break;
        }
    }

    private void TrackFromTrackedResult(ITrackedResult tracked)
    {
        foreach (var aggregate in tracked.Aggregates)
        {
            tracker.Track(aggregate);
        }
    }

    private void TrackDirectAggregate(IAggregateRoot aggregate)
    {
        tracker.Track(aggregate);
    }

    private void TrackFromEnumerable(IEnumerable enumerable)
    {
        foreach (var item in enumerable)
        {
            if (item is IAggregateRoot agg)
                tracker.Track(agg);
        }
    }

    private void TrackFromPublicProperties(object result)
    {
        var props = result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
            ProcessPropertyInfo(prop, result);
    }

    private void ProcessPropertyInfo(PropertyInfo prop, object? result)
    {
        var value = prop.GetValue(result);
        if (value is IAggregateRoot propAggregate)
        {
            tracker.Track(propAggregate);
        }
        else if (value is System.Collections.IEnumerable nestedEnumerable)
        {
            foreach (var item in nestedEnumerable)
            {
                if (item is IAggregateRoot nestedAgg)
                    tracker.Track(nestedAgg);
            }
        }
    }
}