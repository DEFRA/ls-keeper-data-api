using System.Text.Json;

namespace DataConverter.Logic;

public class GenericCsvConverter
{
    public async Task<string> Convert<T>(string csvInputPath, Func<string[], T> mapFunction)
    {
        if (!File.Exists(csvInputPath))
        {
            throw new FileNotFoundException($"Source CSV file not found.", csvInputPath);
        }

        var lines = await File.ReadAllLinesAsync(csvInputPath);
        var records = new List<T>();

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Split(',');
            var record = mapFunction(parts);
            records.Add(record);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(records, options);
    }
}