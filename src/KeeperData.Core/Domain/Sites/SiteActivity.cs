using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteActivity : ValueObject
{
    public string Id { get; private set; }
    public string? Activity { get; private set; }
    public string? Description { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }

    public SiteActivity(
        string id,
        string? activity,
        string? description,
        DateTime? startDate,
        DateTime? endDate,
        DateTime lastUpdatedDate)
    {
        Id = id;
        Activity = activity;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static SiteActivity Create(
        string? activity,
        string? description,
        DateTime? startDate,
        DateTime? endDate)
    {
        return new SiteActivity(
            Guid.NewGuid().ToString(),
            activity,
            description,
            startDate,
            endDate,
            DateTime.UtcNow);
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string? activity,
        string? description,
        DateTime? startDate,
        DateTime? endDate)
    {
        var changed = false;

        changed |= Change(Activity, activity, v => Activity = v, lastUpdatedDate);
        changed |= Change(Description, description, v => Description = v, lastUpdatedDate);
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
        yield return Activity ?? string.Empty;
        yield return Description ?? string.Empty;
        yield return StartDate ?? default;
        yield return EndDate ?? default;
    }
}