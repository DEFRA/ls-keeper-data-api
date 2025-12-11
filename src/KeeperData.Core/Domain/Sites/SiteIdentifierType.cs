using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class SiteIdentifierType : ValueObject
{
    public string Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public SiteIdentifierType(
        string id,
        string code,
        string name,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Name = name;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static SiteIdentifierType Create(
        string siteIdentifierId,
        string code,
        string name,
        DateTime? lastUpdatedDate)
    {
        return new SiteIdentifierType(
            siteIdentifierId,
            code,
            name,
            lastUpdatedDate);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}
