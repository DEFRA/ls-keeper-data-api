using KeeperData.Core.Domain.BuildingBlocks;

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

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string code,
        string name,
        string? longName,
        bool euTradeMember,
        bool devolvedAuthority)
    {
        var changed = false;

        changed |= Change(Code, code, v => Code = v, lastUpdatedDate);
        changed |= Change(Name, name, v => Name = v, lastUpdatedDate);
        changed |= Change(LongName, longName, v => LongName = v, lastUpdatedDate);
        changed |= Change(EuTradeMember, euTradeMember, v => EuTradeMember = v, lastUpdatedDate);
        changed |= Change(DevolvedAuthority, devolvedAuthority, v => DevolvedAuthority = v, lastUpdatedDate);

        return changed;
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        LastUpdatedDate = lastUpdatedAt;
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code ?? string.Empty;
        yield return Name;
        yield return LongName ?? string.Empty;
        yield return EuTradeMember;
        yield return DevolvedAuthority;
    }
}