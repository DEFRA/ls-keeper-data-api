using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Shared;

public class Country : EntityObject
{
    public string Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? LongName { get; private set; }
    public bool EuTradeMemberFlag { get; private set; }
    public bool DevolvedAuthorityFlag { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public Country(
        string id,
        string code,
        string name,
        string? longName,
        bool euTradeMemberFlag,
        bool devolvedAuthorityFlag,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Code = code;
        Name = name;
        LongName = longName;
        EuTradeMemberFlag = euTradeMemberFlag;
        DevolvedAuthorityFlag = devolvedAuthorityFlag;
        LastUpdatedDate = lastUpdatedDate;
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string code,
        string name,
        string? longName,
        bool euTradeMemberFlag,
        bool devolvedAuthorityFlag)
    {
        var changed = false;

        changed |= Change(Code, code, v => Code = v, lastUpdatedDate);
        changed |= Change(Name, name, v => Name = v, lastUpdatedDate);
        changed |= Change(LongName, longName, v => LongName = v, lastUpdatedDate);
        changed |= Change(EuTradeMemberFlag, euTradeMemberFlag, v => EuTradeMemberFlag = v, lastUpdatedDate);
        changed |= Change(DevolvedAuthorityFlag, devolvedAuthorityFlag, v => DevolvedAuthorityFlag = v, lastUpdatedDate);

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
        yield return EuTradeMemberFlag;
        yield return DevolvedAuthorityFlag;
    }
}