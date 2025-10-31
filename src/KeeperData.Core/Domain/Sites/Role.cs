using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class Role : ValueObject
{
    public string Id { get; }
    public string Name { get; }
    public DateTime? LastUpdatedDate { get; }

    public Role(string id, string name, DateTime? lastUpdatedDate)
    {
        Id = id;
        Name = name;
        LastUpdatedDate = lastUpdatedDate;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
    }
}