using KeeperData.Core.Domain.Sites; 
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class RolesToPartyDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Role { get; set; }
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];
    public DateTime? LastUpdatedDate { get; set; }

    public static RolesToPartyDocument FromDomain(RolesToParty m) => new()
    {
        IdentifierId = m.Id,
        Role = m.Role?.Name,
        SpeciesManagedByRole = m.SpeciesManagedByRole.Select(ManagedSpeciesDocument.FromDomain).ToList(),
        LastUpdatedDate = m.LastUpdatedDate
    };


    public RolesToParty ToDomain()
    {
        var roleObject = this.Role is not null
            ? new Role(Guid.NewGuid().ToString(), this.Role, this.LastUpdatedDate)
            : null;

        return new RolesToParty(
            IdentifierId,
            roleObject,
            SpeciesManagedByRole.Select(s => s.ToDomain()),
            LastUpdatedDate
        );
    }
}