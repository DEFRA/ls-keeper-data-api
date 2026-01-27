using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Shared;

public class ManagedSpecies : EntityObject
{
    public string Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }

    public ManagedSpecies(
        string id,
        string code,
        string name,
        DateTime startDate,
        DateTime? endDate,
        DateTime lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static ManagedSpecies Create(
        string code,
        string name,
        DateTime startDate,
        DateTime? endDate)
    {
        return new ManagedSpecies(
            Guid.NewGuid().ToString(),
            code,
            name,
            startDate,
            endDate,
            DateTime.UtcNow
        );
    }

    public bool ApplyChanges(
        string code,
        string name,
        DateTime startDate,
        DateTime? endDate,
        DateTime lastUpdatedDate)
    {
        var changed = false;

        changed |= Change(Code, code, v => Code = v);
        changed |= Change(Name, name, v => Name = v);
        changed |= Change(StartDate, startDate, v => StartDate = v);
        changed |= Change(EndDate, endDate, v => EndDate = v);

        if (changed)
        {
            LastUpdatedDate = lastUpdatedDate;
        }

        return changed;
    }

    private static bool Change<T>(T currentValue, T newValue, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
        yield return Name;
        yield return StartDate;
        yield return EndDate ?? default;
    }
}