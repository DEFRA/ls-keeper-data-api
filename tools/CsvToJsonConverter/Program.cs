using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// This record matches the full schema and the target JSON structure
public record CountryJson(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("longName")] string LongName,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("euTradeMember")] bool EuTradeMember,
    [property: JsonPropertyName("devolvedAuthority")] bool DevolvedAuthority,
    [property: JsonPropertyName("sortOrder")] int SortOrder,
    [property: JsonPropertyName("effectiveStartDate")] DateTime EffectiveStartDate,
    [property: JsonPropertyName("effectiveEndDate")] DateTime? EffectiveEndDate,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdDate")] DateTime CreatedDate,
    [property: JsonPropertyName("lastModifiedBy")] string? LastModifiedBy,
    [property: JsonPropertyName("lastModifiedDate")] DateTime? LastModifiedDate
);

public class Program
{
    public static async Task Main(string[] args)
    {
        const string csvInputPath = "countries.csv";
        const string jsonOutputPath = "countries_generated.json";

        Console.WriteLine("--- CSV to JSON Country Data Generator (Full Schema) ---");

        if (!File.Exists(csvInputPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nERROR: Input file '{csvInputPath}' not found.");
            Console.ResetColor();
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvInputPath);
        var countries = new List<CountryJson>();

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split(',');
            if (parts.Length < 14)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"WARNING: Skipping malformed line with only {parts.Length} columns: {line}");
                Console.ResetColor();
                continue;
            }

            var country = new CountryJson(
                Id: Guid.NewGuid().ToString(),            // Always generate a new GUID, ignore column 1
                Code: parts[0].Trim(),
                LongName: parts[2].Trim(),
                Name: parts[3].Trim(),
                CreatedBy: parts[4].Trim(),
                CreatedDate: ParseDateTime(parts[5], DateTime.UtcNow), 
                DevolvedAuthority: ParseBool(parts[6]),
                EffectiveEndDate: ParseNullableDateTime(parts[7]),
                EffectiveStartDate: ParseDateTime(parts[8], new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                EuTradeMember: ParseBool(parts[9]),
                IsActive: ParseBool(parts[10]),
                LastModifiedBy: string.IsNullOrWhiteSpace(parts[11]) ? null : parts[11].Trim(),
                LastModifiedDate: DateTime.UtcNow,
                SortOrder: ParseInt(parts[13])
            );
            countries.Add(country);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonString = JsonSerializer.Serialize(countries, options);
        await File.WriteAllTextAsync(jsonOutputPath, jsonString);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nSUCCESS: Generated '{jsonOutputPath}' with {countries.Count} records.");
        Console.ResetColor();
        Console.WriteLine($"   -> Located at: {Path.GetFullPath(jsonOutputPath)}");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nACTION REQUIRED:");
        Console.WriteLine("1. Manually copy the newly generated file to the main project.");
        Console.WriteLine(@"2. Place it at: ..\src\KeeperData.Infrastructure\Data\Seed\countries.json");
        Console.WriteLine("3. Commit the new 'countries.json' file to your Git repository.");
        Console.ResetColor();
    }
    private static bool ParseBool(string value, bool defaultValue = false) =>
        bool.TryParse(value.Trim(), out var result) ? result : defaultValue;

    private static int ParseInt(string value, int defaultValue = 0) =>
        int.TryParse(value.Trim(), out var result) ? result : defaultValue;

    private static DateTime ParseDateTime(string value, DateTime defaultValue) =>
        DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result) ? result.ToUniversalTime() : defaultValue;

    private static DateTime? ParseNullableDateTime(string value) =>
        DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result) ? (DateTime?)result.ToUniversalTime() : null;
}