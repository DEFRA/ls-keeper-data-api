using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Domain.Sites.DomainEvents;

namespace KeeperData.Core.Domain.Sites;

public class Site : IAggregateRoot
{
    public string Id { get; private set; }
    public string SystemId { get; private set; }
    public string Type { get; private set; }
    public string Name { get; private set; }
    public string State { get; private set; }

    private readonly List<SiteIdentifier> _siteIdentifiers = [];
    public IReadOnlyCollection<SiteIdentifier> SiteIdentifiers => _siteIdentifiers.AsReadOnly();

    public Site(string id, string systemId, string type, string name, string state)
    {
        Id = id;
        SystemId = systemId;
        Type = type;
        Name = name;
        State = state;
        _domainEvents.Add(new SiteCreatedDomainEvent(Id));
    }

    public static Site Create(string systemId, string type, string name, string state)
    {
        return new Site(Guid.NewGuid().ToString(), systemId, type, name, state);
    }

    public void AddSiteIdentifier(string systemId, string identifier, string type, string? id = null)
    {
        var siteIdentifier = id is null
            ? SiteIdentifier.Create(systemId, identifier, type)
            : new SiteIdentifier(id, systemId, identifier, type);

        _siteIdentifiers.Add(siteIdentifier);
    }

    public void RemoveSiteIdentifier(string identifier)
    {
        var existing = _siteIdentifiers.FirstOrDefault(x => x.Identifier == identifier);
        if (existing is not null)
        {
            _siteIdentifiers.Remove(existing);
        }
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    public void ClearDomainEvents() => _domainEvents.Clear();
    private readonly List<IDomainEvent> _domainEvents = [];
}