using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CsvToJsonConverter.Logic;
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

public class Converter
{
    public string ConvertCsvToJson(string[] csvLines)
    {
        var countries = new List<CountryJson>();

        foreach (var line in csvLines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split(',');
            if (parts.Length < 14) continue;

            var country = new CountryJson(
                Id: Guid.NewGuid().ToString(),
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
        return JsonSerializer.Serialize(countries, options);
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