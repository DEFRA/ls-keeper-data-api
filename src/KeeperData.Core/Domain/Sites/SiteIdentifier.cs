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

    public void Update(
        string identifier,
        string type)
    {
        LastUpdatedDate = DateTime.UtcNow;
        Identifier = identifier;
        Type = type;
    }

    public void UpdateLastUpdatedDate(DateTime lastUpdatedDate)
    {
        LastUpdatedDate = lastUpdatedDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Identifier;
        yield return Type;
    }
}