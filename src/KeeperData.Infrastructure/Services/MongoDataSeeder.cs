using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference; // Required for the new CountryListDocument
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Text.Json;
using File = System.IO.File;

namespace KeeperData.Infrastructure.Services;

public class MongoDataSeeder : IHostedService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MongoDataSeeder> _logger;
    private readonly IMongoCollection<CountryListDocument> _collection;

    // Static fields to help mitigate race conditions in a single process.
    private static DateTime _lastRun = DateTime.MinValue;
    private static readonly object _lock = new object();

    public MongoDataSeeder(
        IWebHostEnvironment env,
        ILogger<MongoDataSeeder> logger,
        IMongoClient client,
        IOptions<MongoConfig> config)
    {
        _env = env;
        _logger = logger;

        _collection = client.GetDatabase(config.Value.DatabaseName).GetCollection<CountryListDocument>("refCountries");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mongo DB Seeder Service is running (Single-Document Replace Mode).");

        lock (_lock)
        {
            if (DateTime.UtcNow < _lastRun.AddMinutes(3))
            {
                _logger.LogInformation("Seeding was performed less than 3 minutes ago within this process. Skipping this run.");
                return;
            }
            _lastRun = DateTime.UtcNow;
        }

        var targetJsonPath = Path.Combine(_env.ContentRootPath, "Data", "Seed", "countries.json");

        if (!File.Exists(targetJsonPath))
        {
            _logger.LogWarning("Seed file '{FileName}' not found. Skipping Mongo data seed.", Path.GetFileName(targetJsonPath));
            return;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(targetJsonPath, cancellationToken);
            var countries = JsonSerializer.Deserialize<List<CountryDocument>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (countries == null || !countries.Any())
            {
                _logger.LogWarning("No data found in '{FileName}'. Skipping Mongo data seed.", Path.GetFileName(targetJsonPath));
                return;
            }


            var countryListDocument = new CountryListDocument
            {
                Countries = countries,
                LastUpdatedDate = DateTime.UtcNow
            };

            var filter = Builders<CountryListDocument>.Filter.Eq(x => x.Id, "all-countries");
            var options = new ReplaceOptions { IsUpsert = true };

            _logger.LogInformation("Replacing 'all-countries' document in '{CollectionName}' collection...", _collection.CollectionNamespace.CollectionName);
            await _collection.ReplaceOneAsync(filter, countryListDocument, options, cancellationToken);
            _logger.LogInformation("Mongo data replacement complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding data from JSON into MongoDB.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}