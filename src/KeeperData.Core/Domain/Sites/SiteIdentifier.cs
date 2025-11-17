using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteIdentifier(
    string id,
    DateTime lastUpdatedDate,
    string identifier,
    string type) : ValueObject
{
    public string Id { get; private set; } = id;
    public DateTime LastUpdatedDate { get; private set; } = lastUpdatedDate;
    public string Identifier { get; private set; } = identifier;
    public string Type { get; private set; } = type;

    public static SiteIdentifier Create(
        string identifier,
        string type)
    {
        return new SiteIdentifier(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            identifier,
            type);
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string identifier,
        string type)
    {
        var changed = false;

        changed |= Change(Identifier, identifier, v => Identifier = v, lastUpdatedDate);
        changed |= Change(Type, type, v => Type = v, lastUpdatedDate);

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
        yield return Identifier;
        yield return Type;
    }
}