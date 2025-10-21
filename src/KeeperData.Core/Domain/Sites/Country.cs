using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Repositories;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;

namespace KeeperData.Core.Domain.Sites;

public class Country : ValueObject
{
    public string Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? LongName { get; private set; }
    public bool EuTradeMember { get; private set; }
    public bool DevolvedAuthority { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }
    public Country(
        string id,
        string code,
        string name,
        string? longName,
        bool euTradeMember,
        bool devolvedAuthority,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Name = name;
        LongName = longName;
        EuTradeMember = euTradeMember;
        DevolvedAuthority = devolvedAuthority; 
        LastUpdatedDate = lastUpdatedDate;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code ?? string.Empty;
    }
}