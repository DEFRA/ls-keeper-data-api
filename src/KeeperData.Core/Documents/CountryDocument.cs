using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("refCountries")]
public class CountryDocument : INestedEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? LongName { get; set; }
    public bool? EuTradeMemberFlag { get; set; }
    public bool? DevolvedAuthorityFlag { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public static CountryDocument FromDomain(Country m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        LongName = m.LongName,
        EuTradeMemberFlag = m.EuTradeMemberFlag,
        DevolvedAuthorityFlag = m.DevolvedAuthorityFlag,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public Country ToDomain() => new(IdentifierId, Code, Name, LongName, EuTradeMemberFlag, DevolvedAuthorityFlag, LastUpdatedDate);
}