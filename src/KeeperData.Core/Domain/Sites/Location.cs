using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class Location(
    string id,
    DateTime lastUpdatedDate,
    string? osMapReference,
    double? easting,
    double? northing) : ValueObject
{
    public string Id { get; private set; } = id;
    public DateTime LastUpdatedDate { get; private set; } = lastUpdatedDate;
    public string? OsMapReference { get; private set; } = osMapReference;
    public double? Easting { get; private set; } = easting;
    public double? Northing { get; private set; } = northing;

    public static Location Create(
        string? osMapReference,
        double? easting,
        double? northing)
    {
        return new Location(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            osMapReference,
            easting,
            northing);
    }

    public void Update(
        string? osMapReference,
        double? easting,
        double? northing)
    {
        LastUpdatedDate = DateTime.UtcNow;
        OsMapReference = osMapReference;
        Easting = easting;
        Northing = northing;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return OsMapReference ?? string.Empty;
        yield return Easting ?? default!;
        yield return Northing ?? default!;
    }
}