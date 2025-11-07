using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: CountyParishHoldingNumber, LocationName, SpeciesTypeCode, SecondaryCph
/// </summary>
[CollectionName("samHoldings")]
public class SamHoldingDocument : BaseHoldingDocument, IEntity, IDeletableEntity, IContainsIndexes
{
    public string? Id { get; set; }
    public int LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public bool Deleted { get; set; }

    public string? CphRelationshipType { get; set; }
    public string? SecondaryCph { get; set; }
    public string? DiseaseType { get; set; }
    public decimal? Interval { get; set; }
    public string? IntervalUnitOfTime { get; set; }
    public string? PremiseSubActivityTypeCode { get; set; }
    public string? MovementRestrictionReasonCode { get; set; }
    public string? SpeciesTypeCode { get; set; }
    public List<string> ProductionUsageCodeList { get; set; } = [];

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CountyParishHoldingNumber"),
                new CreateIndexOptions { Name = "idx_cphNumber" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("AlternativeHoldingIdentifier"),
                new CreateIndexOptions { Name = "idx_altIdentifier" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("LocationName"),
                new CreateIndexOptions { Name = "idx_locationName" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("SpeciesTypeCode"),
                new CreateIndexOptions { Name = "idx_speciesTypeCode" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("SecondaryCph"),
                new CreateIndexOptions { Name = "idx_secondaryCph" })
        ];
    }
}