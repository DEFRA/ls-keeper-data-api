using FluentAssertions;
using KeeperData.Core.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace KeeperData.Core.Tests.Unit.Serialization;

public class UprnSerializerTests
{
    private readonly UprnSerializer _sut = new();

    [Fact]
    public void Serialize_WithNullValue_WritesNull()
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        var context = BsonSerializationContext.CreateRoot(writer);

        writer.WriteStartDocument();
        writer.WriteName("uprn");
        _sut.Serialize(context, new BsonSerializationArgs(), null);
        writer.WriteEndDocument();

        document["uprn"].BsonType.Should().Be(BsonType.Null);
    }

    [Fact]
    public void Serialize_WithStringValue_WritesString()
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        var context = BsonSerializationContext.CreateRoot(writer);

        writer.WriteStartDocument();
        writer.WriteName("uprn");
        _sut.Serialize(context, new BsonSerializationArgs(), "123456789");
        writer.WriteEndDocument();

        document["uprn"].BsonType.Should().Be(BsonType.String);
        document["uprn"].AsString.Should().Be("123456789");
    }

    [Fact]
    public void Deserialize_WithNullBsonType_ReturnsNull()
    {
        var document = new BsonDocument { { "uprn", BsonNull.Value } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithStringBsonType_ReturnsString()
    {
        var document = new BsonDocument { { "uprn", "987654321" } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be("987654321");
    }

    [Fact]
    public void Deserialize_WithInt32BsonType_ConvertsToString()
    {
        var document = new BsonDocument { { "uprn", 25962203 } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be("25962203");
    }

    [Fact]
    public void Deserialize_WithInt64BsonType_ConvertsToString()
    {
        var document = new BsonDocument { { "uprn", 9876543210123L } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be("9876543210123");
    }

    [Fact]
    public void Deserialize_WithDoubleBsonType_ConvertsToStringWithoutDecimals()
    {
        var document = new BsonDocument { { "uprn", 123456.789 } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be("123457");
    }

    [Fact]
    public void Deserialize_WithUnsupportedBsonType_ThrowsBsonSerializationException()
    {
        var document = new BsonDocument { { "uprn", new BsonArray { 1, 2, 3 } } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        
        var act = () => _sut.Deserialize(context, new BsonDeserializationArgs());

        act.Should().Throw<BsonSerializationException>()
            .WithMessage("Cannot deserialize UPRN from BsonType Array");
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(123, "123")]
    [InlineData(25962203, "25962203")]
    [InlineData(-1, "-1")]
    public void Deserialize_WithVariousInt32Values_ConvertsCorrectly(int input, string expected)
    {
        var document = new BsonDocument { { "uprn", input } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("ABC123", "ABC123")]
    [InlineData("  123  ", "  123  ")]
    public void Serialize_Deserialize_RoundTrip_WithStringValues(string input, string expected)
    {
        var document = new BsonDocument();
        using var writer = new BsonDocumentWriter(document);
        var writeContext = BsonSerializationContext.CreateRoot(writer);

        writer.WriteStartDocument();
        writer.WriteName("uprn");
        _sut.Serialize(writeContext, new BsonSerializationArgs(), input);
        writer.WriteEndDocument();

        using var reader = new BsonDocumentReader(document);
        var readContext = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(readContext, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be(expected);
    }

    [Fact]
    public void Deserialize_WithMaxInt32Value_ConvertsToString()
    {
        var document = new BsonDocument { { "uprn", int.MaxValue } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be(int.MaxValue.ToString());
    }

    [Fact]
    public void Deserialize_WithMaxInt64Value_ConvertsToString()
    {
        var document = new BsonDocument { { "uprn", long.MaxValue } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be(long.MaxValue.ToString());
    }

    [Fact]
    public void Deserialize_WithDouble_RemovesDecimalPoints()
    {
        var document = new BsonDocument { { "uprn", 25962203.0 } };
        using var reader = new BsonDocumentReader(document);
        var context = BsonDeserializationContext.CreateRoot(reader);

        reader.ReadStartDocument();
        reader.ReadName();
        var result = _sut.Deserialize(context, new BsonDeserializationArgs());
        reader.ReadEndDocument();

        result.Should().Be("25962203");
    }
}