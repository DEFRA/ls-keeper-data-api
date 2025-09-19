using KeeperData.Core.Domain.BuildingBlocks.Aggregates;

namespace KeeperData.Infrastructure.Database.Transactions;

public class AggregateTracker : IAggregateTracker
{
    private readonly HashSet<IAggregateRoot> _tracked = [];

    private readonly object _lock = new();

    public void Track(IAggregateRoot aggregate)
    {
        if (aggregate == null) return;
        lock (_lock)
        {
            _tracked.Add(aggregate);
        }
    }

    public IEnumerable<IAggregateRoot> GetTrackedAggregates() => _tracked;

    public void Clear() => _tracked.Clear();
}