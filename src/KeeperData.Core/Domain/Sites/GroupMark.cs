using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.Shared;

namespace KeeperData.Core.Domain.Sites;

public class GroupMark : ValueObject
{
    public string Id { get; private set; }
    public string Mark { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public Species? Species { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }

    public GroupMark(
        string id,
        DateTime lastUpdatedDate,
        string mark,
        DateTime startDate,
        DateTime? endDate,
        Species? species)
    {
        Id = id;
        Mark = mark;
        StartDate = startDate;
        EndDate = endDate;
        Species = species;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static GroupMark Create(
        string mark,
        DateTime startDate,
        DateTime? endDate = null,
        Species? species = null)
    {
        return new GroupMark(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            mark,
            startDate,
            endDate,
            species
        );
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string mark,
        DateTime startDate,
        DateTime? endDate,
        Species? species)
    {
        var changed = false;

        changed |= Change(Mark, mark, v => Mark = v, lastUpdatedDate);
        changed |= Change(StartDate, startDate, v => StartDate = v, lastUpdatedDate);
        changed |= Change(EndDate, endDate, v => EndDate = v, lastUpdatedDate);
        changed |= Change(Species, species, v => Species = v, lastUpdatedDate);

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

        if (Species is not null)
        {
            foreach (var component in Species.GetEqualityComponents())
                yield return component;
        }
        else
        {
            yield return string.Empty;
        }

        yield return StartDate;
        yield return EndDate ?? default;
    }
}