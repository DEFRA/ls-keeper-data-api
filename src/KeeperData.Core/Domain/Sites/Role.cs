using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class Role : ValueObject
{
    public string RoleId { get; private set; }
    public string Name { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }

    public Role(
        string roleId,
        string name,
        DateTime? startDate,
        DateTime? endDate,
        DateTime lastUpdatedDate)
    {
        RoleId = roleId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static Role Create(
        string roleId,
        string name,
        DateTime? startDate,
        DateTime? endDate)
    {
        return new Role(
            roleId,
            name,
            startDate,
            endDate,
            DateTime.UtcNow
        );
    }

    public bool ApplyChanges(
        string roleId,
        string name,
        DateTime? startDate,
        DateTime? endDate,
        DateTime lastUpdatedDate)
    {
        var changed = false;

        changed |= Change(RoleId, roleId, v => RoleId = v);
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
        yield return RoleId;
        yield return Name;
        yield return StartDate ?? default;
        yield return EndDate ?? default;
    }
}