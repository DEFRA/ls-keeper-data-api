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

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}