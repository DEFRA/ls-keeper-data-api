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
    [InlineData("lastName", "asc", "{ \"lastName\" : 1 }")]
    [InlineData("lastName", "desc", "{ \"lastName\" : -1 }")]
    [InlineData("lastName", null, "{ \"lastName\" : 1 }")]
    [InlineData(null, null, "{ \"lastName\" : 1 }")]
    [InlineData("partyType", "asc", "{ \"partyType\" : 1 }")]
    [InlineData("state", "desc", "{ \"state\" : -1 }")]
    // [InlineData("invalid", "asc", "{ \"type\" : 1 }")] // TODO why
    public void Build_ShouldReturnCorrectSortDefinition(string? order, string? sort, string expected)
    { //TODO - no notes in swagger about allowable values
        var query = new GetPartiesQuery { Order = order, Sort = sort };
        var sortDefinition = PartySortBuilder.Build(query);

        var renderedSort = sortDefinition.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry);

        renderedSort.Should().BeEquivalentTo(BsonDocument.Parse(expected));
    }
}