using KeeperData.Core.Domain.BuildingBlocks;

namespace KeeperData.Core.Domain.Shared;

public class Location : ValueObject
{
    public string Id { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string? OsMapReference { get; private set; }
    public double? Easting { get; private set; }
    public double? Northing { get; private set; }
    public Address? Address { get; private set; }
    public List<Communication> Communication { get; private set; } = [];

    public Location(
        string id,
        DateTime lastUpdatedDate,
        string? osMapReference,
        double? easting,
        double? northing,
        Address? address,
        IEnumerable<Communication>? communication)
    {
        Id = id;
        LastUpdatedDate = lastUpdatedDate;
        OsMapReference = osMapReference;
        Easting = easting;
        Northing = northing;
        Address = address;
        Communication = communication?.ToList() ?? [];
    }

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

    public bool ApplyChanges(
        DateTime lastUpdatedDate,
        string? osMapReference,
        double? easting,
        double? northing,
        Address? newAddress,
        IEnumerable<Communication>? newCommunication)
    {
        var changed = false;

        changed |= Change(OsMapReference, osMapReference, v => OsMapReference = v, lastUpdatedDate);
        changed |= Change(Easting, easting, v => Easting = v, lastUpdatedDate);
        changed |= Change(Northing, northing, v => Northing = v, lastUpdatedDate);

        if (newAddress is not null)
        {
            changed |= ChangeAddress(Address, lastUpdatedDate, newAddress);
        }

        if (newCommunication is not null)
        {
            var newComms = newCommunication.ToList();
            if (!Communication.SequenceEqual(newComms))
            {
                Communication = newComms;
                changed = true;
            }
        }

        if (changed)
        {
            LastUpdatedDate = lastUpdatedDate;
        }

        return changed;
    }

    private bool ChangeAddress(Address? existing, DateTime currentDateTime, Address incoming)
    {
        if (existing is null)
        {
            Address = incoming;
            return true;
        }

        if (!existing.GetEqualityComponents().SequenceEqual(incoming.GetEqualityComponents()))
        {
            return existing.ApplyChanges(
                lastUpdatedDate: currentDateTime,
                uprn: incoming.Uprn,
                addressLine1: incoming.AddressLine1,
                addressLine2: incoming.AddressLine2,
                postTown: incoming.PostTown,
                county: incoming.County,
                postCode: incoming.PostCode,
                country: incoming.Country);
        }

        return false;
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
        yield return OsMapReference ?? string.Empty;
        yield return Easting ?? 0.0;
        yield return Northing ?? 0.0;

        if (Address is not null)
            yield return Address;

        foreach (var comm in Communication.OrderBy(c => c.Id))
            yield return comm;
    }
}