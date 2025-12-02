using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace KeeperData.Core.Repositories;

/// <summary>
/// Apply this attribute to a property on a document to generate an index (default ascending) referencing the field by its BsonElement name, with index name "idx_{BsonElementName}"
/// </summary>
public class AutoIndexed : Attribute
{
    /// <summary>
    /// Generate the AutoIndexes for a document, for Properties marked with the [AutoIndex] attribute
    /// </summary>
    /// <typeparam name="T">Document Class</typeparam>
    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels<T>() where T : IContainsIndexes
    {
        return typeof(T)
        .GetMembers()
        .Where(member => Attribute.IsDefined(member, typeof(AutoIndexed)))
        .Select(member =>
        {
            var bsonElemAttr = member.GetCustomAttribute<BsonElementAttribute>();
            if (bsonElemAttr == null)
                throw new ApplicationException($"BsonElementAttribute must be defined for property {member.Name} on {member.DeclaringType?.Name}");

            return new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending(bsonElemAttr.ElementName),
                    new CreateIndexOptions { Name = $"idx_{bsonElemAttr.ElementName}" });
        });
    }
}