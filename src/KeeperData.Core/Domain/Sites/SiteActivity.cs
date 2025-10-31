using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Exceptions;

namespace KeeperData.Core.Domain.Sites;

public class SiteActivity : ValueObject
{
    public string Id { get; private set; }
    public string Activity { get; private set; }
    public string? Description { get; private set; }
    //Schema says this is nullable but required???
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? LastUpdatedDate { get; private set; }

    public SiteActivity(string id, string activity, string? description, DateTime startDate, DateTime? endDate, DateTime? lastUpdatedDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainException("EndDate cannot be before StartDate.");
        }

        Id = id;
        Activity = activity;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        LastUpdatedDate = lastUpdatedDate;
    }

    public static SiteActivity Create(string activity, string? description, DateTime startDate, DateTime? endDate)
    {
        return new SiteActivity(
            Guid.NewGuid().ToString(),
            activity,
            description,
            startDate,
            endDate,
            DateTime.UtcNow);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Activity;
        yield return StartDate;
    }
}