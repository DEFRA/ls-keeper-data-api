using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Reflection;

namespace KeeperData.Core.Attributes;

/// <summary>
/// Apply this attribute to a property on a document to generate an index (default ascending) referencing the field by its BsonElement name, with index name "idxv2_{BsonElementName}"
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class AutoIndexedAttribute : Attribute
{
    /// <summary>
    /// Generate the AutoIndexes for a document, for Properties marked with the [AutoIndex] attribute
    /// </summary>
    /// <typeparam name="T">Document Class</typeparam>
    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels<T>() where T : IContainsIndexes
    {
        return typeof(T)
            .GetMembers()
            .Where(member => IsDefined(member, typeof(AutoIndexedAttribute)))
            .Select(member =>
            {
                var bsonElemAttr = member.GetCustomAttribute<BsonElementAttribute>();
                return new CreateIndexModel<BsonDocument>(
                        Builders<BsonDocument>.IndexKeys.Ascending(bsonElemAttr?.ElementName),
                        new CreateIndexOptions { Name = $"idxv2_{bsonElemAttr?.ElementName}" });
            });
    }
}