using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("sites")]
[BsonIgnoreExtraElements]
public class SiteDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string Id { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    [JsonIgnore]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The timestamp of the last time the Site record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    [AutoIndexed]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// The type of site (e.g. Agricultural Holding, Market).
    /// </summary>
    [BsonElement("type")]
    [JsonPropertyName("type")]
    public SiteTypeSummaryDocument? Type { get; set; }

    /// <summary>
    /// The name of the site.
    /// </summary>
    /// <example>Hansel &amp; Gretel Farm</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    [AutoIndexed]
    public string Name { get; set; } = default!;

    /// <summary>
    /// The current state of the site.
    /// </summary>
    [BsonElement("state")]
    [JsonPropertyName("state")]
    [AutoIndexed]
    public string? State { get; set; }

    /// <summary>
    /// The date the site was established.
    /// </summary>
    [BsonElement("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The date the site was decommissioned, if applicable.
    /// </summary>
    [BsonElement("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The source system of the data (e.g. SAM, CTS).
    /// </summary>
    /// <example>SAM</example>
    [BsonElement("source")]
    [JsonPropertyName("source")]
    [AutoIndexed]
    public string? Source { get; set; }

    [BsonElement("parentSiteIdentifier")]
    [JsonPropertyName("parentSiteIdentifier")]
    public string? ParentSiteIdentifier { get; set; }

    [BsonElement("holdingType")]
    [JsonPropertyName("holdingType")]
    public string? HoldingType { get; set; }

    [BsonElement("permanentLandHoldingIdentifier")]
    [JsonPropertyName("permanentLandHoldingIdentifier")]
    public string? PermanentLandHoldingIdentifier { get; set; }

    /// <summary>
    /// Indicates whether identity documents should be destroyed for this site.
    /// </summary>
    [BsonElement("destroyIdentityDocumentsFlag")]
    [JsonPropertyName("destroyIdentityDocumentsFlag")]
    public bool? DestroyIdentityDocumentsFlag { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    [JsonIgnore]
    [AutoIndexed]
    public bool Deleted { get; set; }

    /// <summary>
    /// The geographic location of the site.
    /// </summary>
    [BsonElement("location")]
    [JsonPropertyName("location")]
    public LocationDocument? Location { get; set; }

    /// <summary>
    /// The identifiers associated with this site (e.g. CPH Number).
    /// </summary>
    [BsonElement("identifiers")]
    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDocument> Identifiers { get; set; } = [];

    /// <summary>
    /// The parties associated with this site.
    /// </summary>
    [BsonElement("parties")]
    [JsonPropertyName("parties")]
    public List<SitePartyDocument> Parties { get; set; } = [];

    /// <summary>
    /// The species registered at this site.
    /// </summary>
    [BsonElement("species")]
    [JsonPropertyName("species")]
    public List<SpeciesSummaryDocument> Species { get; set; } = [];

    /// <summary>
    /// The herd/flock/group marks associated with this site.
    /// </summary>
    [BsonElement("marks")]
    [JsonPropertyName("marks")]
    public List<GroupMarkDocument> Marks { get; set; } = [];

    /// <summary>
    /// The activities associated with this site.
    /// </summary>
    [BsonElement("activities")]
    [JsonPropertyName("activities")]
    public List<SiteActivityDocument> Activities { get; set; } = [];

    [BsonElement("localAuthorityName")]
    [JsonPropertyName("localAuthorityName")]
    public string? LocalAuthorityName { get; set; }

    [BsonElement("associatedMainHoldings")]
    [JsonPropertyName("associatedMainHoldings")]
    public List<AssociatedHoldingDocument> AssociatedMainHoldings { get; set; } = [];

    [BsonElement("associatedCommonLands")]
    [JsonPropertyName("associatedCommonLands")]
    public List<AssociatedHoldingDocument> AssociatedCommonLands { get; set; } = [];

    [BsonElement("effectiveFromDate")]
    [JsonPropertyName("effectiveFromDate")]
    public DateTime? EffectiveFromDate { get; set; }

    [BsonElement("effectiveToDate")]
    [JsonPropertyName("effectiveToDate")]
    public DateTime? EffectiveToDate { get; set; }

    [BsonElement("approvalCurrentFlag")]
    [JsonPropertyName("approvalCurrentFlag")]
    public bool? ApprovalCurrentFlag { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<SiteDocument>().Concat(
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("type.code"),
                new CreateIndexOptions { Name = "idxv2_type_code", Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("identifiers.identifier"),
                new CreateIndexOptions { Name = "idxv2_identifiers_identifier", Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("parties.id"),
                new CreateIndexOptions { Name = "idxv2_parties_id", Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("parties.customerNumber"),
                new CreateIndexOptions { Name = "idxv2_parties_customerNumber", Sparse = true }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("parties.communication.email"),
                new CreateIndexOptions { Name = "idxv2_parties_communication_email", Collation = IndexDefaults.CollationCaseInsensitive, Sparse = true }),
        ]);
    }
}