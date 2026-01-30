using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.Shared;

namespace KeeperData.Core.Domain.Sites;

public class GroupMark : EntityObject
{
    public string Id { get; private set; }
    public string Mark { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public List<Species> Species { get; private set; } = [];
    public DateTime LastUpdatedDate { get; private set; }

    public GroupMark(
        string id,
        DateTime lastUpdatedDate,
        string mark,
        DateTime startDate,
        DateTime? endDate,
        IEnumerable<Species>? species)
    {
        Id = id;
        Mark = mark;
        StartDate = startDate;
        EndDate = endDate;
        Species = species?.ToList() ?? [];
        LastUpdatedDate = lastUpdatedDate;
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string mark,
        DateTime startDate,
        DateTime? endDate,
        IEnumerable<Species>? species)
    {
        var changed = false;

        changed |= Change(Mark, mark, v => Mark = v, lastUpdatedDate);
        changed |= Change(StartDate, startDate, v => StartDate = v, lastUpdatedDate);
        changed |= Change(EndDate, endDate, v => EndDate = v, lastUpdatedDate);

        var newSpeciesList = species?.ToList() ?? [];

        if (Species.Count != newSpeciesList.Count || !Species.SequenceEqual(newSpeciesList))
        {
            Species = newSpeciesList;
            LastUpdatedDate = lastUpdatedDate;
            changed = true;
        }

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
        yield return Mark;
        yield return StartDate;
        yield return EndDate ?? default;

        yield return Species.Count;

        foreach (var s in Species.OrderBy(x => x.Code))
        {
            foreach (var component in s.GetEqualityComponents())
                yield return component;
        }
    }
}