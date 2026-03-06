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
    [InlineData("lastname", "asc", "{ \"lastname\" : 1 }")]
    [InlineData("lastname", "desc", "{ \"lastname\" : -1 }")]
    [InlineData("lastname", null, "{ \"lastname\" : 1 }")]
    [InlineData(null, null, "{ \"lastname\" : 1 }")]
    [InlineData("id", "asc", "{ \"id\" : 1 }")]
    [InlineData("id", "desc", "{ \"id\" : -1 }")]
    [InlineData("id", null, "{ \"id\" : 1 }")]
    public void Build_ShouldReturnCorrectSortDefinition(string? order, string? sort, string expected)
    {
        var query = new GetPartiesQuery { Order = order, Sort = sort };
        var sortDefinition = PartySortBuilder.Build(query);

        var renderedSort = sortDefinition.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry);

        renderedSort.Should().BeEquivalentTo(BsonDocument.Parse(expected));
    }
}