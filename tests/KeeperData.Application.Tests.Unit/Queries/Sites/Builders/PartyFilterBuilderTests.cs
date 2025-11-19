using FluentAssertions;
using KeeperData.Application.Queries.Parties;
using KeeperData.Application.Queries.Parties.Builders;
using KeeperData.Core.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace KeeperData.Application.Tests.Unit.Queries.Parties.Builders;

public class PartyFilterBuilderTests
{
    static PartyFilterBuilderTests()
    {
        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);
    }

    [Fact]
    public void Build_ShouldReturnEmptyFilter_WhenQueryIsEmpty()
    {
        var query = new GetPartiesQuery();
        var filter = PartyFilterBuilder.Build(query);

        filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry)
            .Should().BeEquivalentTo(new BsonDocument());
    }

    [Fact]
    public void Build_ShouldCreateFilterForFirstName()
    {
        var query = new GetPartiesQuery { FirstName = "Trevor" };
        var filter = PartyFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry);

        renderedFilter["firstName"].AsString.Should().BeEquivalentTo("Trevor");
    }

    [Fact]
    public void Build_ShouldCreateFilterForLastName()
    {
        var query = new GetPartiesQuery { LastName = "Smith" };
        var filter = PartyFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry);

        renderedFilter["lastName"].AsString.Should().BeEquivalentTo("Smith");
    }

    /*
    TODO EMAIL not in dataset
        [Fact]
        public void Build_ShouldCreateFilterForEmail()
        {
            var query = new GetPartiesQuery { Email = "t.s@gmail.com" };
            var filter = PartyFilterBuilder.Build(query);
            var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry);

            renderedFilter["email"].Should().BeEquivalentTo("t.s@gmail.com");
        }
        */
}