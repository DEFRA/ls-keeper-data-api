using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// A startup service that generates a baseline JSON file from a CSV,
/// but only if the JSON file does not already exist.
/// </summary>
public class JsonGenerationService : IHostedService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<JsonGenerationService> _logger;

    // Just the properties we need for now
    private record CountryJson(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("name")] string Name
    );

    public JsonGenerationService(IWebHostEnvironment env, ILogger<JsonGenerationService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JSON Generation Service is running.");

        var sourceCsvPath = Path.Combine(_env.ContentRootPath, "Data", "Source", "countries.csv");
        var targetJsonPath = Path.Combine(_env.ContentRootPath, "Data", "Seed", "countries.json");

        if (File.Exists(targetJsonPath))
        {
            _logger.LogInformation("'{FileName}' already exists. Skipping generation.", Path.GetFileName(targetJsonPath));
            return Task.CompletedTask;
        }

        if (!File.Exists(sourceCsvPath))
        {
            _logger.LogWarning("Source file '{FileName}' not found. Cannot generate target JSON.", Path.GetFileName(sourceCsvPath));
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogInformation("'{TargetFile}' not found. Generating from '{SourceFile}'...", Path.GetFileName(targetJsonPath), Path.GetFileName(sourceCsvPath));

            var lines = File.ReadAllLines(sourceCsvPath);
            var countries = lines
                .Skip(1) // Skip header
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new CountryJson(
                        Guid.NewGuid().ToString(),
                        parts[0].Trim(),
                        parts[1].Trim()
                    );
                })
                .ToList();

            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(countries, options);

            var outputDir = Path.GetDirectoryName(targetJsonPath);
            if (outputDir != null && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.WriteAllText(targetJsonPath, jsonString);

            _logger.LogInformation("Successfully generated '{FileName}' with {Count} records.", Path.GetFileName(targetJsonPath), countries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating JSON from CSV.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}