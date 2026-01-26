using FluentAssertions;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Tests.Unit;

public class AutoIndexedAttributeTests
{
    [CollectionName("testdoc")]
    public class TestDoc : IEntity, IContainsIndexes
    {
        [JsonPropertyName("id")]
        [BsonElement("id")]
        [AutoIndexed]
        public required string Id { get; set; }

        public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
        {
            return AutoIndexedAttribute.GetIndexModels<PartyDocument>();
        }
    }

    [CollectionName("testdoc2")]
    public class TestDoc2 : IEntity, IContainsIndexes
    {
        [JsonPropertyName("id")]
        [BsonElement("id")]
        public required string Id { get; set; }

        [JsonPropertyName("a")]
        [BsonElement("a")]
        [AutoIndexed]
        public required string A { get; set; }

        [JsonPropertyName("b")]
        [BsonElement("b")]
        [AutoIndexed]
        public required string B { get; set; }

        public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
        {
            return AutoIndexedAttribute.GetIndexModels<PartyDocument>();
        }
    }

    [Fact]
    public void ShouldIndexDecoratedProperty()
    {
        var results = AutoIndexedAttribute.GetIndexModels<TestDoc>().ToList();
        results.Count.Should().Be(1);
        var result = results.Single();
        var expected = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending(
                "id"),
            new CreateIndexOptions { Name = $"idxv2_id" });
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldIndexDecoratedProperties()
    {
        var results = AutoIndexedAttribute.GetIndexModels<TestDoc2>().ToList();
        results.Count.Should().Be(2);
        var expected = new List<CreateIndexModel<BsonDocument>> {new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending( "a"), new CreateIndexOptions { Name = $"idxv2_a" }),
         new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending( "b"), new CreateIndexOptions { Name = $"idxv2_b" })};
        results.Should().BeEquivalentTo(expected);
    }
}