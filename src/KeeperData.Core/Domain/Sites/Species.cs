using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class Species : ValueObject
{
    public string Id { get; }
    public string Code { get; }
    public string Name { get; }
    public DateTime? LastUpdatedDate { get; }

    public Species(string id, string code, string name, DateTime? lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Name = name;
        LastUpdatedDate = lastUpdatedDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }
}