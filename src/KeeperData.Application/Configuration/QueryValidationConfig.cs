namespace KeeperData.Application.Configuration;

public class QueryValidationConfig<T> : QueryValidationConfig;

public class QueryValidationConfig
{
    public static string SectionName = "QueryValidation";
    public string? ValidatorType { get; set; }
    public int MaxPageSize { get; set; } = 500;
    public int MaxQueryableTypes { get; set; } = 50;
}