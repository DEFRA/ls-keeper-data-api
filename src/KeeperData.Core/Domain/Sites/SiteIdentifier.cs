using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteIdentifier(
    string id,
    DateTime lastUpdatedDate,
    string identifier,
    SiteIdentifierType type) : EntityObject
{
    public string Id { get; private set; } = id;
    public DateTime LastUpdatedDate { get; private set; } = lastUpdatedDate;
    public string Identifier { get; private set; } = identifier;
    public SiteIdentifierType Type { get; private set; } = type;

    public static SiteIdentifier Create(
        string identifier,
        SiteIdentifierType type)
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
        SiteIdentifierType newType)
    {
        var changed = false;

        if (!EqualityComparer<string>.Default.Equals(Identifier, identifier))
        {
            Identifier = identifier;
            changed = true;
        }

        if (!EqualityComparer<SiteIdentifierType>.Default.Equals(Type, newType))
        {
            Type = newType;
            changed = true;
        }

        if (changed)
        {
            LastUpdatedDate = lastUpdatedDate;
        }

        return changed;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}