using KeeperData.Core.Documents;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// A startup service that reads a JSON data file and upserts its
/// contents into a MongoDB collection.
/// </summary>
public class MongoDataSeeder : IHostedService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MongoDataSeeder> _logger;
    private readonly IMongoDatabase _database;

    public MongoDataSeeder(
        IWebHostEnvironment env,
        ILogger<MongoDataSeeder> logger,
        IMongoClient client,
        IOptions<MongoConfig> config)
    {
        _env = env;
        _logger = logger;
        _database = client.GetDatabase(config.Value.DatabaseName);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mongo DB Seeder Service is running.");

        // Could put this in appsettings
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

            var collection = _database.GetCollection<CountryDocument>("refCountries");
            var bulkOps = new List<WriteModel<CountryDocument>>();

            foreach (var country in countries)
            {
                // match documents by the country code
                var filter = Builders<CountryDocument>.Filter.Eq(x => x.Code, country.Code);

                var update = Builders<CountryDocument>.Update
                    .SetOnInsert(x => x.IdentifierId, country.IdentifierId) // Set ID only on creation
                    .Set(x => x.Name, country.Name)
                    .Set(x => x.LastUpdatedDate, DateTime.UtcNow);

                var upsertOne = new UpdateOneModel<CountryDocument>(filter, update) { IsUpsert = true };
                bulkOps.Add(upsertOne);
            }

            if (bulkOps.Any())
            {
                _logger.LogInformation("Upserting {Count} records into '{CollectionName}'...", bulkOps.Count, collection.CollectionNamespace.CollectionName);
                await collection.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken);
                _logger.LogInformation("Mongo data seeding complete.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding data from JSON into MongoDB.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}