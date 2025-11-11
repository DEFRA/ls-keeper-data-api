using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents;

[CollectionName("siteGroupMarkRelationships")]
public class SiteGroupMarkRelationshipDocument : IEntity, IContainsIndexes, IDeletableEntity
{
    public string? Id { get; set; }
    public int? LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public bool Deleted { get; set; }

    public string PartyId { get; set; } = string.Empty;
    public string PartyTypeId { get; set; } = string.Empty; // LOV Lookup / Internal Id
    public bool IsHolder { get; set; }

    public string Herdmark { get; set; } = string.Empty;
    public string CountyParishHoldingHerd { get; set; } = string.Empty;

    public string HoldingIdentifier { get; set; } = string.Empty;
    public string HoldingIdentifierType { get; set; } = string.Empty;

    public string? RoleTypeId { get; set; } // LOV Lookup / Internal Id
    public string? RoleTypeName { get; set; } // LOV Lookup / Internal Name

    public string? SpeciesTypeId { get; set; } // LOV Lookup / Internal Id
    public string? SpeciesTypeCode { get; set; }

    public string? ProductionUsageId { get; set; } // LOV Lookup / Internal Id
    public string? ProductionUsageCode { get; set; }

    public string? ProductionTypeId { get; set; } // LOV Lookup / Internal Id
    public string? ProductionTypeCode { get; set; }

    public string? DiseaseType { get; set; }
    public decimal? Interval { get; set; }
    public string? IntervalUnitOfTime { get; set; }

    public DateTime GroupMarkStartDate { get; set; } = default;
    public DateTime? GroupMarkEndDate { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifier"),
                new CreateIndexOptions { Name = "idx_holdingIdentifier" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("HoldingIdentifierType"),
                new CreateIndexOptions { Name = "idx_holdingIdentifierType" })
        ];
    }
}