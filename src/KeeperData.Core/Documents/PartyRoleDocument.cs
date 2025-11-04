using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PartyRoleDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleDocument FromDomain(PartyRole m) => new()
    {
        IdentifierId = m.Id,
        RoleId = m.Role.RoleId,
        Role = m.Role.Name,
        StartDate = m.Role.StartDate ?? default,
        EndDate = m.Role.EndDate,
        SpeciesManagedByRole = [.. m.SpeciesManagedByRole.Select(ManagedSpeciesDocument.FromDomain)],
        LastUpdatedDate = m.LastUpdatedDate
    };

    public PartyRole ToDomain()
    {
        var role = new Role(
            roleId: RoleId,
            name: Role,
            startDate: StartDate,
            endDate: EndDate,
            lastUpdatedDate: LastUpdatedDate ?? DateTime.UtcNow
        );

        var species = SpeciesManagedByRole.Select(s => s.ToDomain());

        return new PartyRole(
            IdentifierId,
            role,
            species,
            LastUpdatedDate
        );
    }
}