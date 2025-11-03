using DataConverter.Logic;
using DataConverter.Models;
using System.Globalization;

public class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        var converter = new GenericCsvConverter();
        var dataType = args[0].ToLower();
        string jsonString;
        string inputPath = "";
        string outputPath = "";

        try
        {
            switch (dataType)
            {
                case "countries":
                    inputPath = "countries.csv";
                    outputPath = "countries_generated.json";
                    jsonString = await converter.Convert<CountryJson>(inputPath, MapCountry);
                    break;

                case "species":
                    inputPath = "species.csv";
                    outputPath = "species_generated.json";
                    jsonString = await converter.Convert<SpeciesJson>(inputPath, MapSpecies);
                    break;

                case "roles":
                    inputPath = "roles.csv";
                    outputPath = "roles_generated.json";
                    jsonString = await converter.Convert<RoleJson>(inputPath, MapRole);
                    break;

                case "premisestypes":
                    inputPath = "premisestypes.csv";
                    outputPath = "premisestypes_generated.json";
                    jsonString = await converter.Convert<PremisesTypeJson>(inputPath, MapPremisesType);
                    break;

                case "premisesactivitytypes":
                    inputPath = "premisesactivitytypes.csv";
                    outputPath = "premisesactivitytypes_generated.json";
                    jsonString = await converter.Convert<PremisesActivityTypeJson>(inputPath, MapPremisesActivityType);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nERROR: Unknown data type '{dataType}'.");
                    PrintUsage();
                    return;
            }

            await File.WriteAllTextAsync(outputPath, jsonString);
            PrintSuccess(outputPath, inputPath);
        }
        catch (FileNotFoundException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nERROR: Input file '{inputPath}' was not found.");
            Console.WriteLine("Please place the CSV file in the root of the converter project at the following location:");
            Console.WriteLine($" -> {Path.GetFullPath(inputPath)}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn unexpected error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }

    public static CountryJson MapCountry(string[] parts)
    {
        if (parts.Length < 14) throw new InvalidDataException("CSV line for country has fewer than 14 columns.");

        return new CountryJson(
            Id: Guid.NewGuid().ToString(),
            Code: parts[0].Trim(),
            LongName: parts[2].Trim(),
            Name: parts[3].Trim(),
            CreatedBy: parts[4].Trim(),
            CreatedDate: ParseDateTime(parts[5], DateTime.UtcNow),
            DevolvedAuthority: ParseBool(parts[6]),
            EffectiveEndDate: ParseNullableDateTime(parts[7]),
            EffectiveStartDate: ParseDateTime(parts[8], DateTime.UtcNow),
            EuTradeMember: ParseBool(parts[9]),
            IsActive: ParseBool(parts[10]),
            LastModifiedBy: string.IsNullOrWhiteSpace(parts[11]) ? null : parts[11].Trim(),
            LastModifiedDate: HandlePlaceholderOrEmptyDate(parts[12], DateTime.UtcNow),
            SortOrder: ParseInt(parts[13])
        );
    }

    public static SpeciesJson MapSpecies(string[] parts)
    {
        if (parts.Length < 11) throw new InvalidDataException("CSV line for species has fewer than 11 columns.");

        return new SpeciesJson(
            Id: HandlePlaceholderId(parts[1]),
            Code: parts[0].Trim(),
            Name: parts[2].Trim(),
            CreatedBy: parts[3].Trim(),
            CreatedDate: HandlePlaceholderOrEmptyDate(parts[4], DateTime.UtcNow),
            EffectiveEndDate: ParseNullableDateTime(parts[5]),
            EffectiveStartDate: HandlePlaceholderOrEmptyDate(parts[6], DateTime.UtcNow),
            IsActive: ParseBool(parts[7]),
            SortOrder: ParseInt(parts[8]),
            LastModifiedBy: parts[9].Trim(),
            LastModifiedDate: HandlePlaceholderOrEmptyDate(parts[10], DateTime.UtcNow)
        );
    }

    public static RoleJson MapRole(string[] parts)
    {
        if (parts.Length < 11) throw new InvalidDataException("CSV line for party role has fewer than 11 columns.");

        return new RoleJson(
            Id: HandlePlaceholderId(parts[1]),
            Code: parts[0].Trim(),
            Name: parts[2].Trim(),
            CreatedBy: parts[3].Trim(),
            CreatedDate: HandlePlaceholderOrEmptyDate(parts[4], DateTime.UtcNow),
            EffectiveEndDate: ParseNullableDateTime(parts[5]),
            EffectiveStartDate: HandlePlaceholderOrEmptyDate(parts[6], DateTime.UtcNow),
            IsActive: ParseBool(parts[7]),
            SortOrder: ParseInt(parts[8]),
            LastModifiedBy: parts[9].Trim(),
            LastModifiedDate: HandlePlaceholderOrEmptyDate(parts[10], DateTime.UtcNow)
        );
    }

    public static PremisesTypeJson MapPremisesType(string[] parts)
    {
        if (parts.Length < 11) throw new InvalidDataException("CSV line for premises type has fewer than 11 columns.");

        return new PremisesTypeJson(
            Id: HandlePlaceholderId(parts[1]),
            Code: parts[0].Trim(),
            Name: parts[2].Trim(),
            CreatedBy: parts[3].Trim(),
            CreatedDate: HandlePlaceholderOrEmptyDate(parts[4], DateTime.UtcNow),
            EffectiveEndDate: ParseNullableDateTime(parts[5]),
            EffectiveStartDate: HandlePlaceholderOrEmptyDate(parts[6], DateTime.UtcNow),
            IsActive: ParseBool(parts[7]),
            SortOrder: ParseInt(parts[8]),
            LastModifiedBy: parts[9].Trim(),
            LastModifiedDate: HandlePlaceholderOrEmptyDate(parts[10], DateTime.UtcNow)
        );
    }

    public static PremisesActivityTypeJson MapPremisesActivityType(string[] parts)
    {
        if (parts.Length < 11) throw new InvalidDataException("CSV line for premises activity type has fewer than 11 columns.");

        return new PremisesActivityTypeJson(
            Id: HandlePlaceholderId(parts[1]),
            Code: parts[0].Trim(),
            Name: parts[2].Trim(),
            CreatedBy: parts[3].Trim(),
            CreatedDate: HandlePlaceholderOrEmptyDate(parts[4], DateTime.UtcNow),
            EffectiveEndDate: ParseNullableDateTime(parts[5]),
            EffectiveStartDate: HandlePlaceholderOrEmptyDate(parts[6], DateTime.UtcNow),
            IsActive: ParseBool(parts[7]),
            PriorityOrder: ParseInt(parts[8]),
            LastModifiedBy: parts[9].Trim(),
            LastModifiedDate: HandlePlaceholderOrEmptyDate(parts[10], DateTime.UtcNow)
        );
    }

    private static void PrintUsage()
    {
        Console.WriteLine("\nUsage: dotnet run <data_type>");
        Console.WriteLine("Available data types: countries, species, roles, premisesactivitytypes");
    }

    private static void PrintSuccess(string outputPath, string inputPath)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nSUCCESS: Generated '{outputPath}' from '{inputPath}'.");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\nACTION REQUIRED:");
        Console.WriteLine($"1. Manually copy '{outputPath}' to the main project.");
        Console.WriteLine(@"   (e.g., ..\src\KeeperData.Infrastructure\Data\Seed\...)");
        Console.WriteLine("2. Rename the file if necessary (e.g., to 'countries.json').");
        Console.WriteLine("3. Commit the new JSON file to your Git repository.");
        Console.ResetColor();
    }
    private static string HandlePlaceholderId(string value) => value.Trim().Equals("NEWID()",
        StringComparison.OrdinalIgnoreCase) ? Guid.NewGuid().ToString() : value.Trim();
    private static DateTime HandlePlaceholderOrEmptyDate(string value, DateTime defaultValue)
    {
        var trimmedValue = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmedValue) || trimmedValue.Equals("NEWDATE()", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.UtcNow;
        }

        return ParseDateTime(trimmedValue, defaultValue);
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