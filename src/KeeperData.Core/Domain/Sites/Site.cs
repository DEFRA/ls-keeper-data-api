using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Sites.DomainEvents;

namespace KeeperData.Core.Domain.Sites;

public class Site : IAggregateRoot
{
    public string Id { get; private set; }
    public int LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string Type { get; private set; }
    public string Name { get; private set; }
    public string State { get; private set; }
    public bool Deleted { get; set; }

    private readonly List<SiteIdentifier> _identifiers = [];
    public IReadOnlyCollection<SiteIdentifier> Identifiers => _identifiers.AsReadOnly();

    private Location? _location;
    public Location? Location => _location;

    public string? PrimaryIdentifier => Identifiers.FirstOrDefault()?.Identifier;

    public Site(
        string id,
        int batchId,
        DateTime lastUpdatedDate,
        string type,
        string name,
        string state,
        Location? location = null)
    {
        Id = id;
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = lastUpdatedDate;
        Type = type;
        Name = name;
        State = state;
        Deleted = false;
        _location = location;
        _domainEvents.Add(new SiteCreatedDomainEvent(Id));
    }

    public static Site Create(
        int batchId,
        string type,
        string name,
        string state,
        Location? location = null)
    {
        return new Site(
            Guid.NewGuid().ToString(),
            batchId,
            DateTime.UtcNow,
            type,
            name,
            state,
            location);
    }

    public void UpdateLastUpdatedDate(DateTime lastUpdatedDate)
    {
        LastUpdatedDate = lastUpdatedDate;
    }

    public void AddSiteIdentifier(
        DateTime lastUpdatedDate,
        string identifier,
        string type,
        string? id = null)
    {
        var siteIdentifier = id is null
            ? SiteIdentifier.Create(
                identifier,
                type)
            : new SiteIdentifier(
                id,
                lastUpdatedDate,
                identifier,
                type);

        _identifiers.Add(siteIdentifier);
    }

    public void RemoveSiteIdentifier(string identifier)
    {
        var existing = _identifiers.FirstOrDefault(x => x.Identifier == identifier);
        if (existing is not null)
        {
            _identifiers.Remove(existing);
        }
    }

    public void SetLocation(
        DateTime lastUpdatedDate,
        string? osMapReference,
        double? easting,
        double? northing,
        string? id = null)
    {
        _location = id is null
            ? Location.Create(
                osMapReference,
                easting,
                northing)
            : new Location(
                id,
                lastUpdatedDate,
                osMapReference,
                easting,
                northing);
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    public void ClearDomainEvents() => _domainEvents.Clear();
    private readonly List<IDomainEvent> _domainEvents = [];
}