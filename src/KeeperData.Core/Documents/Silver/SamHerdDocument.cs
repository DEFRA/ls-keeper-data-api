using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Core.Documents.Silver;

[CollectionName("samHerds")]
public class SamHerdDocument : IEntity, IContainsIndexes, IDeletableEntity
{
    public string? Id { get; set; }
    public int LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public bool Deleted { get; set; }

    public string Herdmark { get; set; } = string.Empty;
    public string CountyParishHoldingHerd { get; set; } = string.Empty;
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    public string? SpeciesTypeId { get; set; } // LOV Lookup / Internal Id
    public string? SpeciesTypeCode { get; set; }

    public string? ProductionUsageId { get; set; } // LOV Lookup / Internal Id
    public string? ProductionUsageCode { get; set; }

    public string? ProductionTypeId { get; set; } // LOV Lookup / Internal Id
    public string? ProductionTypeCode { get; set; }

    public decimal? Interval { get; set; }
    public string? IntervalUnitOfTime { get; set; }

    public DateTime GroupMarkStartDate { get; set; } = default;
    public DateTime? GroupMarkEndDate { get; set; }

    public List<string> KeeperPartyIdList { get; set; } = [];
    public List<string> OwnerPartyIdList { get; set; } = [];

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Herdmark"),
                new CreateIndexOptions { Name = "idx_herdmark" }),

            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("CountyParishHoldingNumber"),
                new CreateIndexOptions { Name = "idx_countyParishHoldingNumber" })
        ];
    }
}