using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace KeeperData.Core.Serialization;

public class UprnSerializer : SerializerBase<string?>
{
    public override string? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();
        
        switch (bsonType)
        {
            case BsonType.Null:
                context.Reader.ReadNull();
                return null;
            case BsonType.String:
                return context.Reader.ReadString();
            case BsonType.Int32:
                return context.Reader.ReadInt32().ToString();
            case BsonType.Int64:
                return context.Reader.ReadInt64().ToString();
            case BsonType.Double:
                return context.Reader.ReadDouble().ToString("F0");
            default:
                throw new BsonSerializationException($"Cannot deserialize UPRN from BsonType {bsonType}");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            context.Writer.WriteString(value);
        }
    }
}