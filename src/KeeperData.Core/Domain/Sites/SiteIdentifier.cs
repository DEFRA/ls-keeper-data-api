using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteIdentifier(string id, string systemId, string identifier, string type) : ValueObject
{
    public string Id { get; private set; } = id;
    public string SystemId { get; private set; } = systemId;
    public string Identifier { get; private set; } = identifier;
    public string Type { get; private set; } = type;

    public static SiteIdentifier Create(string systemId, string identifier, string type)
    {
        return new SiteIdentifier(Guid.NewGuid().ToString(), systemId, identifier, type);
    }

    public void Update(string identifier, string type)
    {
        Identifier = identifier;
        Type = type;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SystemId;
        yield return Identifier;
        yield return Type;
    }
}