using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;

namespace KeeperData.Core.Documents;

[CollectionName("sites")]
public class SiteDocument : IEntity
{
    public required string Id { get; set; }
    public string SystemId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string State { get; set; } = default!;
    public List<SiteIdentifierDocument> SiteIdentifiers { get; private set; } = [];

    public static SiteDocument FromDomain(Site site) => new()
    {
        Id = site.Id,
        SystemId = site.SystemId,
        Type = site.Type,
        Name = site.Name,
        State = site.State,
        SiteIdentifiers = [.. site.SiteIdentifiers.Select(SiteIdentifierDocument.FromDomain)]
    };

    public Site ToDomain()
    {
        var site = new Site(Id, SystemId, Type, Name, State);

        if (SiteIdentifiers is not null)
        {
            foreach (var item in SiteIdentifiers)
            {
                site.AddSiteIdentifier(item.SystemId, item.Identifier, item.Type, item.Id);
            }
        }

        return site;
    }
}