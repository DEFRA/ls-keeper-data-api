using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Sites.DomainEvents;
using KeeperData.Core.Exceptions;

namespace KeeperData.Core.Domain.Sites;

public class Site : IAggregateRoot
{
    public string Id { get; private set; }
    public int LastUpdatedBatchId { get; private set; }
    public DateTime LastUpdatedDate { get; private set; }
    public string Type { get; private set; }
    public string Name { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? State { get; private set; }
    public string? Source { get; private set; }
    public bool? DestroyIdentityDocumentsFlag { get; private set; }
    public bool Deleted { get; private set; }

    private readonly List<SiteIdentifier> _identifiers = [];
    public IReadOnlyCollection<SiteIdentifier> Identifiers => _identifiers.AsReadOnly();

    private Location? _location;
    public Location? Location => _location;

    private readonly List<SiteParty> _parties = [];
    public IReadOnlyCollection<SiteParty> Parties => _parties.AsReadOnly();

    private readonly List<Species> _species = [];
    public IReadOnlyCollection<Species> Species => _species.AsReadOnly();

    private readonly List<GroupMark> _marks = [];
    public IReadOnlyCollection<GroupMark> Marks => _marks.AsReadOnly();

    private readonly List<SiteActivity> _activities = [];
    public IReadOnlyCollection<SiteActivity> Activities => _activities.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public Site(
        string id,
        int batchId,
        DateTime lastUpdatedDate,
        string type,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted,
        Location? location)
    {
        Id = id;
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = lastUpdatedDate;
        Type = type;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        State = state;
        Source = source;
        DestroyIdentityDocumentsFlag = destroyIdentityDocumentsFlag;
        Deleted = deleted;
        _location = location;
    }

    public static Site Create(
        int batchId,
        string type,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted,
        Location? location = null)
    {
        var site = new Site(
            Guid.NewGuid().ToString(),
            batchId,
            DateTime.UtcNow,
            type,
            name,
            startDate,
            endDate,
            state,
            source,
            destroyIdentityDocumentsFlag,
            deleted,
            location);

        site._domainEvents.Add(new SiteCreatedDomainEvent(site.Id));
        return site;
    }

    public void Update(
        DateTime lastUpdatedDate,
        int batchId,
        string type,
        string name,
        DateTime startDate,
        DateTime? endDate,
        string? state,
        string? source,
        bool? destroyIdentityDocumentsFlag,
        bool deleted)
    {
        var changed = false;

        changed |= Change(LastUpdatedBatchId, batchId, v => LastUpdatedBatchId = v, lastUpdatedDate);
        changed |= Change(Type, type, v => Type = v, lastUpdatedDate);
        changed |= Change(Name, name, v => Name = v, lastUpdatedDate);
        changed |= Change(StartDate, startDate, v => StartDate = v, lastUpdatedDate);
        changed |= Change(EndDate, endDate, v => EndDate = v, lastUpdatedDate);
        changed |= Change(State, state, v => State = v, lastUpdatedDate);
        changed |= Change(Source, source, v => Source = v, lastUpdatedDate);
        changed |= Change(DestroyIdentityDocumentsFlag, destroyIdentityDocumentsFlag, v => DestroyIdentityDocumentsFlag = v, lastUpdatedDate);
        changed |= Change(Deleted, deleted, v => Deleted = v, lastUpdatedDate);

        if (changed)
        {
            _domainEvents.Add(new SiteUpdatedDomainEvent(Id));
        }
    }

    public void Delete(int batchId)
    {
        if (Deleted) return;

        Deleted = true;
        State = "Inactive";
        LastUpdatedBatchId = batchId;
        LastUpdatedDate = DateTime.UtcNow;
    }

    public void UpdateLocation(
        DateTime lastUpdatedDate,
        string? osMapReference,
        double? easting,
        double? northing,
        Address? address,
        IEnumerable<Communication>? communication)
    {
        if (_location is null)
        {
            _location = Location.Create(osMapReference, easting, northing, address, communication);
            UpdateLastUpdatedDate(lastUpdatedDate);
            return;
        }

        if (_location.ApplyChanges(lastUpdatedDate, osMapReference, easting, northing, address, communication))
        {
            UpdateLastUpdatedDate(lastUpdatedDate);
        }
    }

    public void SetLocation(Location location)
    {
        _location = location;
    }

    public void AddSiteIdentifier(DateTime lastUpdatedDate, string identifier, string type, string? id = null)
    {
        if (_identifiers.Any(i => i.Identifier == identifier && i.Type == type))
        {
            throw new DomainException("Site already contains an identifier with the same type and value.");
        }

        var siteIdentifier = id is null
            ? SiteIdentifier.Create(identifier, type)
            : new SiteIdentifier(id, lastUpdatedDate, identifier, type);

        _identifiers.Add(siteIdentifier);
        UpdateLastUpdatedDate(DateTime.UtcNow);
    }

    private bool Change<T>(T currentValue, T newValue, Action<T> setter, DateTime lastUpdatedAt)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) return false;
        setter(newValue);
        UpdateLastUpdatedDate(lastUpdatedAt);
        return true;
    }

    private void UpdateLastUpdatedDate(DateTime lastUpdatedDate)
    {
        LastUpdatedDate = lastUpdatedDate;
    }
}