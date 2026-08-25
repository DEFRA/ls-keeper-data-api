using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// A user account, keyed on the identity provider subject once the user has logged on at least once.
/// </summary>
[CollectionName("userAccounts")]
public class UserAccountDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the user account.
    /// </summary>
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The identity provider subject claim. Null until the user has logged on for the first time.
    /// </summary>
    /// <example>9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d</example>
    [BsonElement("subject")]
    [JsonPropertyName("subject")]
    [BsonIgnoreIfNull]
    public string? Subject { get; set; }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    /// <example>jane.farmer@example.com</example>
    [BsonElement("email")]
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    /// <summary>
    /// The first name of the user, taken from the identity provider claims.
    /// </summary>
    /// <example>Jane</example>
    [BsonElement("firstName")]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The last name of the user, taken from the identity provider claims.
    /// </summary>
    /// <example>Farmer</example>
    [BsonElement("lastName")]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// The display name of the user, derived from the first and last names.
    /// </summary>
    /// <example>Jane Farmer</example>
    [BsonElement("displayName")]
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The CPH association graph for the user, rebuilt from master data on each logon.
    /// </summary>
    [BsonElement("cphAssociations")]
    [JsonPropertyName("cphAssociations")]
    public List<CphAssociationDocument> CphAssociations { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the CPH association graph was rebuilt.
    /// </summary>
    [BsonElement("associationsRefreshedDate")]
    [JsonPropertyName("associationsRefreshedDate")]
    public DateTime? AssociationsRefreshedDate { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The timestamp of the last time the user account was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    [AutoIndexed]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    [JsonIgnore]
    [AutoIndexed]
    public bool Deleted { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return Enumerable.Concat(
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("subject"),
                new CreateIndexOptions { Name = "uidx_subject", Unique = true, Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("email"),
                new CreateIndexOptions
                {
                    Name = "uidx_email",
                    Unique = true,
                    Collation = IndexDefaults.CollationCaseInsensitive
                })
        ],
        AutoIndexedAttribute.GetIndexModels<UserAccountDocument>());
    }
}
