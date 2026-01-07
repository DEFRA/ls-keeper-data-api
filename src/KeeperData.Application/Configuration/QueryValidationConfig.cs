namespace KeeperData.Application.Configuration;

public class QueryValidationConfig
{
    public static string SectionName = "QueryValidation";
    public int MaxPageSize { get; set; } = 100;
    public int MaxQueryableTypes { get; set; } = 50;
}