using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Shared;
public class Communication : ValueObject
{
    public string Id { get; private set; }
    public string? Email { get; private set; }
    public string? Mobile { get; private set; }
    public string? Landline { get; private set; }
    public bool? PrimaryContactFlag { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }

    public Communication(
        string id,
        DateTime lastUpdatedDate,
        string? email,
        string? mobile,
        string? landline,
        bool? primaryContactFlag)
    {
        Id = id;
        LastUpdatedDate = lastUpdatedDate;
        Email = email;
        Mobile = mobile;
        Landline = landline;
        PrimaryContactFlag = primaryContactFlag;
    }

    public static Communication Create(
        string? email,
        string? mobile,
        string? landline,
        bool? primaryContactFlag)
    {
        return new Communication(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            email,
            mobile,
            landline,
            primaryContactFlag
        );
    }

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string? email,
        string? mobile,
        string? landline,
        bool? primaryContactFlag)
    {
        var changed = false;

        changed |= Change(Email, email, v => Email = v, lastUpdatedDate);
        changed |= Change(Mobile, mobile, v => Mobile = v, lastUpdatedDate);
        changed |= Change(Landline, landline, v => Landline = v, lastUpdatedDate);
        changed |= Change(PrimaryContactFlag, primaryContactFlag, v => PrimaryContactFlag = v, lastUpdatedDate);

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
        yield return Email ?? string.Empty;
        yield return Mobile ?? string.Empty;
        yield return Landline ?? string.Empty;
        yield return PrimaryContactFlag ?? false;
    }
}