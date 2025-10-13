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
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Species code cannot be null or empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Species name cannot be null or empty.", nameof(name));

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