using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteActivity : ValueObject
{
    public string Id { get; private set; }
    public SiteActivityType Type { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }

    public SiteActivity(
        string id,
        SiteActivityType type,
        DateTime? startDate,
        DateTime? endDate,
        DateTime lastUpdatedDate)
    {
        Id = id;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static SiteActivity Create(
        string id,
        SiteActivityType type,
        DateTime? startDate,
        DateTime? endDate,
        DateTime lastUpdatedDate)
    {
        return new SiteActivity(
            id,
            type,
            startDate,
            endDate,
            lastUpdatedDate);
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        SiteActivityType type,
        DateTime? startDate,
        DateTime? endDate)
    {
        var changed = false;

        changed |= Change(Type, type, v => Type = v, lastUpdatedDate);
        changed |= Change(StartDate, startDate, v => StartDate = v, lastUpdatedDate);
        changed |= Change(EndDate, endDate, v => EndDate = v, lastUpdatedDate);

        return changed;
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        LastUpdatedDate = lastUpdatedAt;
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}