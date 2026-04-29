using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Shared;

public class SiteType : EntityObject
{
    public string Id { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public SiteType(
        string id,
        string code,
        string description,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Description = description;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static SiteType Create(
        string id,
        string code,
        string description,
        DateTime? lastUpdatedDate)
    {
        return new SiteType(
            id,
            code,
            description,
            lastUpdatedDate
        );
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
    }
}