using FluentAssertions;
using KeeperData.Application.Queries.Sites;
using KeeperData.Application.Queries.Sites.Builders;
using KeeperData.Core.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace KeeperData.Application.Tests.Unit.Queries.Sites.Builders;

public class SiteFilterBuilderTests
{
    static SiteFilterBuilderTests()
    {
        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);
    }

    [Fact]
    public void Build_ShouldReturnDefaultDeletedFilter_WhenQueryIsEmpty()
    {
        var query = new GetSitesQuery();
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        var expectedBson = BsonDocument.Parse(@"
            {
                ""deleted"": false
            }");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }

    [Fact]
    public void Build_ShouldCreateFilterForSiteIdentifier()
    {
        var query = new GetSitesQuery { SiteIdentifier = "CPH123" };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        var expectedBson = BsonDocument.Parse(@"
            {
                ""deleted"": false,
                ""identifiers"": { ""$elemMatch"": { ""identifier"": ""CPH123"" } }
            }");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }

    [Fact]
    public void Build_ShouldCreateFilterForSiteIdentifiers()
    {
        var query = new GetSitesQuery { SiteIdentifiers = ["CPH123", "CPH456"] };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        var expectedBson = BsonDocument.Parse(@"
            {
                ""deleted"": false,
                ""identifiers.identifier"": { ""$in"": [ ""CPH123"", ""CPH456"" ] }
            }");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }

    [Fact]
    public void Build_ShouldCreateFilterForSiteId()
    {
        var siteId = Guid.NewGuid();
        var query = new GetSitesQuery { SiteId = siteId };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);
        renderedFilter["_id"].AsString.Should().Be(siteId.ToString());
    }

    [Fact]
    public void Build_ShouldCreateFilterForKeeperPartyId()
    {
        // Arrange
        var keeperPartyId = Guid.NewGuid();
        var query = new GetSitesQuery { KeeperPartyId = keeperPartyId };

        // Act
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(),
            BsonSerializer.SerializerRegistry);

        // Assert
        var expectedBson = BsonDocument.Parse($@"
            {{
                ""parties"": {{ ""$elemMatch"": {{ ""customerNumber"": ""{keeperPartyId}"" }} }},
                ""deleted"": false
            }}");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }

    [Fact]
    public void Build_ShouldCombineKeeperPartyIdWithOtherFilters()
    {
        // Arrange
        var keeperPartyId = Guid.NewGuid();
        var query = new GetSitesQuery
        {
            SiteIdentifier = "CPH123",
            KeeperPartyId = keeperPartyId
        };

        // Act
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(),
            BsonSerializer.SerializerRegistry);

        // Assert
        var expectedBson = BsonDocument.Parse($@"
        {{
            ""identifiers"" : {{ ""$elemMatch"" : {{ ""identifier"" : ""CPH123"" }} }},
            ""parties"": {{ ""$elemMatch"": {{ ""customerNumber"": ""{keeperPartyId}"" }} }},
            ""deleted"": false
        }}");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }

    [Fact]
    public void Build_ShouldCreateFilterForType()
    {
        var query = new GetSitesQuery { Type = ["type1", "type2"] };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        renderedFilter["type.code"]["$in"].AsBsonArray.Select(v => v.AsString)
            .Should().BeEquivalentTo(["type1", "type2"]);
    }

    [Fact]
    public void Build_ShouldCombineFiltersWithAnd()
    {
        var query = new GetSitesQuery { SiteIdentifier = "CPH123", Type = ["type1"] };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        var expectedBson = BsonDocument.Parse(@"
        {
            ""deleted"": false,
            ""identifiers"" : { ""$elemMatch"" : { ""identifier"" : ""CPH123"" } },
            ""type"" : { ""$ne"" : null },
            ""type.code"" : { ""$in"" : [""type1""] }
        }");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }

    [Fact]
    public void Build_ShouldCreateFilterForSiteIds()
    {
        var siteId1 = Guid.NewGuid();
        var siteId2 = Guid.NewGuid();
        var query = new GetSitesQuery { SiteIds = [siteId1, siteId2] };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        var expectedIds = new[] { siteId1.ToString(), siteId2.ToString() };

        renderedFilter["_id"]["$in"].AsBsonArray.Select(x => x.AsString)
            .Should().BeEquivalentTo(expectedIds);
    }
}