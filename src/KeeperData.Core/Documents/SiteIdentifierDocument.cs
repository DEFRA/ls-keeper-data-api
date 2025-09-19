using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;

namespace KeeperData.Core.Documents;

public class SiteIdentifierDocument : IEntity
{
    public required string Id { get; set; }
    public string SystemId { get; set; } = default!;
    public string Identifier { get; set; } = default!;
    public string Type { get; set; } = default!;

    public static SiteIdentifierDocument FromDomain(SiteIdentifier si) => new()
    {
        Id = si.Id,
        SystemId = si.SystemId,
        Identifier = si.Identifier,
        Type = si.Type
    };

    public SiteIdentifier ToDomain() => new(Id, SystemId, Identifier, Type);
}