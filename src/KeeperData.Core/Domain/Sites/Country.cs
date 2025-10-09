using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class Country : ValueObject
{
    public string Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? LongName { get; private set; }
    public bool? EuTradeMemberFlag { get; private set; }
    public bool? DevolvedAuthorityFlag { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public Country(string id, string code, string name, string? longName, bool? euTradeMemberFlag, bool? devolvedAuthorityFlag, DateTime? lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Name = name;
        LongName = longName;
        EuTradeMemberFlag = euTradeMemberFlag;
        DevolvedAuthorityFlag = devolvedAuthorityFlag;
        LastUpdatedDate = lastUpdatedDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code ?? string.Empty;
    }
}