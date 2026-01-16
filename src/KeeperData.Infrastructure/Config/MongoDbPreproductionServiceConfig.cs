using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Infrastructure.Config;

[ExcludeFromCodeCoverage]
public class MongoDbPreproductionServiceConfig
{
    public const string SectionName = "MongoDbPreproductionService";
    public bool Enabled { get; set; } = false;
    public string[] PermittedTables { get; set; } = [];
}