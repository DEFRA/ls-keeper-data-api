using FluentAssertions;
using KeeperData.Application.Queries.Parties;
using KeeperData.Application.Queries.Parties.Builders;
using KeeperData.Core.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace KeeperData.Application.Tests.Unit.Queries.Parties.Builders;

public class PartySortBuilderTests
{
    static PartySortBuilderTests()
    {
        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);
    }

    [Theory]
    [InlineData("name", "asc", "{ \"name\" : 1 }")]
    [InlineData("name", "desc", "{ \"name\" : -1 }")]
    [InlineData("name", null, "{ \"name\" : 1 }")]
    [InlineData(null, null, "{ \"name\" : 1 }")]
    [InlineData("id", "asc", "{ \"customerNumber\" : 1 }")]
    [InlineData("id", "desc", "{ \"customerNumber\" : -1 }")]
    [InlineData("id", null, "{ \"customerNumber\" : 1 }")]
    public void Build_ShouldReturnCorrectSortDefinition(string? order, string? sort, string expected)
    {
        var query = new GetPartiesQuery { Order = order, Sort = sort };
        var sortDefinition = PartySortBuilder.Build(query);

        var renderedSort = sortDefinition.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry);

        renderedSort.Should().BeEquivalentTo(BsonDocument.Parse(expected));
    }
}