using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace TsvToJsonConverter.DataConverter;

[ExcludeFromCodeCoverage]
public class GenericTsvConverter
{
    public static async Task<string> Convert<T>(string tsvInputPath, Func<string[], T> mapFunction)
    {
        if (!File.Exists(tsvInputPath))
        {
            throw new FileNotFoundException($"Source TSV file not found.", tsvInputPath);
        }

        var lines = await File.ReadAllLinesAsync(tsvInputPath, Encoding.GetEncoding("iso-8859-1"));
        var records = new List<T>();

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split('\t');
            var record = mapFunction(parts);
            records.Add(record);
        }

        return JsonSerializer.Serialize(records, ConverterJsonDefaults.DefaultOptions);
    }
}

public static class ConverterJsonDefaults
{
    private static JsonSerializerOptions s_defaultOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonSerializerOptions DefaultOptions
    {
        get => s_defaultOptions;
        set => s_defaultOptions = value;
    }
}