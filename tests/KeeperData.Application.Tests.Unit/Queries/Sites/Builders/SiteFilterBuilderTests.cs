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
    public void Build_ShouldReturnEmptyFilter_WhenQueryIsEmpty()
    {
        var query = new GetSitesQuery();
        var filter = SiteFilterBuilder.Build(query);

        filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry)
            .Should().BeEquivalentTo(new BsonDocument());
    }

    [Fact]
    public void Build_ShouldCreateFilterForSiteIdentifier()
    {
        var query = new GetSitesQuery { SiteIdentifier = "CPH123" };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        var expectedBson = BsonDocument.Parse(@"
            {
                ""identifiers"": { ""$elemMatch"": { ""identifier"": ""CPH123"" } }
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
        var keeperPartyId = Guid.NewGuid();
        var query = new GetSitesQuery { KeeperPartyId = keeperPartyId };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        renderedFilter["keeperPartyIds"].AsString.Should().Be(keeperPartyId.ToString());
    }

    [Fact]
    public void Build_ShouldCreateFilterForType()
    {
        var query = new GetSitesQuery { Type = ["type1", "type2"] };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        renderedFilter["type"]["$in"].AsBsonArray.Select(v => v.AsString)
            .Should().BeEquivalentTo(new[] { "type1", "type2" });
    }

    [Fact]
    public void Build_ShouldCombineFiltersWithAnd()
    {
        var query = new GetSitesQuery { SiteIdentifier = "CPH123", Type = ["type1"] };
        var filter = SiteFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        var expectedBson = BsonDocument.Parse(@"
        {
            ""identifiers"" : { ""$elemMatch"" : { ""identifier"" : ""CPH123"" } },
            ""type"" : { ""$in"" : [""type1""] }
        }");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }
}