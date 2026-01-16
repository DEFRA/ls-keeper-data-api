using FluentAssertions;
using KeeperData.Application.Queries.Sites;
using KeeperData.Application.Queries.Sites.Builders;
using KeeperData.Core.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace KeeperData.Application.Tests.Unit.Queries.Sites.Builders;

public class SiteSortBuilderTests
{
    static SiteSortBuilderTests()
    {
        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);
    }

    [Theory]
    [InlineData("name", "asc", "{ \"name\" : 1 }")]
    [InlineData("name", "desc", "{ \"name\" : -1 }")]
    [InlineData("name", null, "{ \"name\" : 1 }")]
    [InlineData("type", "asc", "{ \"type.code\" : 1 }")]
    [InlineData("type", "desc", "{ \"type.code\" : -1 }")]
    [InlineData("type", null, "{ \"type.code\" : 1 }")]
    [InlineData("siteidentifier", "asc", "{ \"identifiers.identifier\" : 1 }")]
    [InlineData("siteidentifier", "desc", "{ \"identifiers.identifier\" : -1 }")]
    [InlineData("siteidentifier", null, "{ \"identifiers.identifier\" : 1 }")]
    [InlineData(null, null, "{ \"name\" : 1 }")]
    public void Build_ShouldReturnCorrectSortDefinition(string? order, string? sort, string expected)
    {
        var query = new GetSitesQuery { Order = order, Sort = sort };
        var sortDefinition = SiteSortBuilder.Build(query);

        var renderedSort = sortDefinition.Render(BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry);

        renderedSort.Should().BeEquivalentTo(BsonDocument.Parse(expected));
    }
}