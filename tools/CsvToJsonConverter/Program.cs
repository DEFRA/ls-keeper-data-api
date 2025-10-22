using CsvToJsonConverter.Logic;
public class Program
{
    public static async Task Main(string[] args)
    {
        const string csvInputPath = "countries.csv";
        const string jsonOutputPath = "countries_generated.json";

        Console.WriteLine("--- CSV to JSON Country Data Generator (Full Schema) ---");
        Console.WriteLine($"Looking for source file: '{Path.GetFullPath(csvInputPath)}'");

        if (!File.Exists(csvInputPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nERROR: Input file '{csvInputPath}' not found.");
            Console.WriteLine($"Please place 'countries.csv' in the same directory as the CsvToJsonConverter project and try again.");
            Console.ResetColor();
            return;
        }

        // 1. Read the source file
        var lines = await File.ReadAllLinesAsync(csvInputPath);

        // 2. Use the converter to process the data
        var converter = new Converter();
        var jsonString = converter.ConvertCsvToJson(lines);
        var countryCount = jsonString.Split(new[] { "\"id\"" }, StringSplitOptions.None).Length - 1;


        // 3. Write the output file
        await File.WriteAllTextAsync(jsonOutputPath, jsonString);

        // 4. Display all messages and instructions to the developer
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nSUCCESS: Generated '{jsonOutputPath}' with {countryCount} records.");
        Console.ResetColor();
        Console.WriteLine($"   -> Located at: {Path.GetFullPath(jsonOutputPath)}");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nACTION REQUIRED:");
        Console.WriteLine("1. Manually copy the newly generated file to the main project.");
        Console.WriteLine(@"2. Place it at: ..\src\KeeperData.Infrastructure\Data\Seed\countries.json");
        Console.WriteLine("3. Review and manually edit the new file if needed.");
        Console.WriteLine("4. Commit the final 'countries.json' file to your Git repository.");
        Console.ResetColor();
    }
}