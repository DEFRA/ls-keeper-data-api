using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Shared;

public class PartyRoleSite : ValueObject
{
    public string Id { get; private set; }
    public string? Name { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public PartyRoleSite(
        string id,
        string? name,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Name = name;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static PartyRoleSite Create(
        string siteId,
        string? name)
    {
        return new PartyRoleSite(
            siteId,
            name,
            DateTime.UtcNow);
    }

    public bool ApplyChanges(string? name, DateTime lastUpdatedDate)
    {
        var changed = false;

        changed |= Change(Name, name, v => Name = v);

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
        yield return Id;
    }
}
