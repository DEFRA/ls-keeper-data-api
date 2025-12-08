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
    public void Build_ShouldReturnDefaultDeletedFilter_WhenQueryIsEmpty()
    {
        var query = new GetPartiesQuery();
        var filter = PartyFilterBuilder.Build(query);
        var renderedFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(), BsonSerializer.SerializerRegistry);

        var expectedBson = BsonDocument.Parse(@"
            {
                ""deleted"": false
            }");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
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

    [Fact]
    public void Build_ShouldCreateFilterForEmail()
    {
        // Arrange
        var email = "test@example.com";
        var query = new GetPartiesQuery { Email = email };

        // Act
        var filter = PartyFilterBuilder.Build(query);
        var renderedFilter = filter.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<PartyDocument>(),
            BsonSerializer.SerializerRegistry);

        // Assert
        var expectedBson = BsonDocument.Parse($@"
            {{
                ""communication"": {{ ""$elemMatch"": {{ ""email"": ""{email}"" }} }},
                ""deleted"": false
            }}");

        renderedFilter.Should().BeEquivalentTo(expectedBson);
    }
}