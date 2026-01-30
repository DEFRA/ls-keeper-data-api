using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteActivityType : EntityObject
{
    public string Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public SiteActivityType(
        string id,
        DateTime? lastUpdatedDate,
        string code,
        string name)
    {
        Id = id;
        Code = code;
        Name = name;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static SiteActivityType Create(
        string id,
        DateTime? lastUpdatedDate,
        string code,
        string name)
    {
        return new SiteActivityType(
            id,
            lastUpdatedDate,
            code,
            name
        );
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string code,
        string name)
    {
        var changed = false;

        changed |= Change(Code, code, v => Code = v, lastUpdatedDate);
        changed |= Change(Name, name, v => Name = v, lastUpdatedDate);

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