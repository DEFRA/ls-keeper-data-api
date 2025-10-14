using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Sites;

public class Location(
    string id,
    DateTime lastUpdatedDate,
    string? osMapReference,
    double? easting,
    double? northing,
    Address? address,
    IEnumerable<Communication>? communication) : ValueObject
{
    public string Id { get; private set; } = id;
    public DateTime LastUpdatedDate { get; private set; } = lastUpdatedDate;
    public string? OsMapReference { get; private set; } = osMapReference;
    public double? Easting { get; private set; } = easting;
    public double? Northing { get; private set; } = northing;
    public Address? Address { get; private set; } = address;
    public IReadOnlyCollection<Communication> Communication { get; private set; } = (communication ?? Enumerable.Empty<Communication>()).ToList().AsReadOnly();

    public static Location Create(
        string? osMapReference,
        double? easting,
        double? northing,
        Address? address,
        IEnumerable<Communication>? communication)
    {
        return new Location(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            osMapReference,
            easting,
            northing,
            address,
            communication);
    }

    public void Update(
        string? osMapReference,
        double? easting,
        double? northing,
        Address? address,
        IEnumerable<Communication>? communication)
    {
        LastUpdatedDate = DateTime.UtcNow;
        OsMapReference = osMapReference;
        Easting = easting;
        Northing = northing;
        Address = address;
        Communication = (communication ?? Enumerable.Empty<Communication>()).ToList().AsReadOnly();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return OsMapReference ?? string.Empty;
        yield return Easting ?? 0.0;
        yield return Northing ?? 0.0;

        if (Address != null)
        {
            yield return Address;
        }

        foreach (var comm in Communication.OrderBy(c => c.Id))
        {
            yield return comm;
        }
    }
}