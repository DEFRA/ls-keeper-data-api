using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;
public class Communication : ValueObject
{
    public string Id { get; private set; }
    public string? Email { get; private set; }
    public string? Mobile { get; private set; }
    public string? Landline { get; private set; }
    public bool? PrimaryContactFlag { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public Communication(
        string id,
        string? email,
        string? mobile,
        string? landline,
        bool? primaryContactFlag,
        DateTime? lastUpdatedDate)
    {
        Id = id;
        Email = email;
        Mobile = mobile;
        Landline = landline;
        PrimaryContactFlag = primaryContactFlag;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static Communication Create(string? email, string? mobile, string? landline, bool? primaryContactFlag)
    {
        return new Communication(
            Guid.NewGuid().ToString(),
            email,
            mobile,
            landline,
            primaryContactFlag,
            DateTime.UtcNow
        );
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email ?? string.Empty;
        yield return Mobile ?? string.Empty;
        yield return Landline ?? string.Empty;
        yield return PrimaryContactFlag ?? false;
    }
}