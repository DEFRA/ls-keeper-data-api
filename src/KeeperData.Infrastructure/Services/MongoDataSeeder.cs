using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Text.Json;
using File = System.IO.File;

namespace KeeperData.Infrastructure.Services;

/// <summary>
/// Seeds MongoDB with reference data from JSON files during application startup.
/// Implements IHostedLifecycleService to ensure seeding completes before other hosted services start.
/// </summary>
public class MongoDataSeeder(
    IWebHostEnvironment env,
    ILogger<MongoDataSeeder> logger,
    IMongoClient client,
    IOptions<MongoConfig> config) : IHostedLifecycleService
{
    private readonly IMongoDatabase _database = client.GetDatabase(config.Value.DatabaseName);

    private static DateTime s_lastRun = DateTime.MinValue;
    private static readonly object s_lock = new();
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Mongo DB Generic Seeder Service is running...");

        lock (s_lock)
        {
            if (DateTime.UtcNow < s_lastRun.AddMinutes(3))
            {
                logger.LogInformation("Seeding was performed less than 3 minutes ago. Skipping this run.");
                return;
            }
            s_lastRun = DateTime.UtcNow;
        }

        try
        {
            await Task.WhenAll(
                SeedAsync<CountryListDocument, CountryDocument>(cancellationToken),
                SeedAsync<SpeciesListDocument, SpeciesDocument>(cancellationToken),
                SeedAsync<RoleListDocument, RoleDocument>(cancellationToken),
                SeedAsync<PremisesTypeListDocument, PremisesTypeDocument>(cancellationToken),
                SeedAsync<PremisesActivityTypeListDocument, PremisesActivityTypeDocument>(cancellationToken),
                SeedAsync<SiteIdentifierTypeListDocument, SiteIdentifierTypeDocument>(cancellationToken),
                SeedAsync<ProductionUsageListDocument, ProductionUsageDocument>(cancellationToken),
                SeedAsync<FacilityBusinessActivityMapListDocument, FacilityBusinessActivityMapDocument>(cancellationToken)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred during the data seeding process.");
        }

        logger.LogInformation("Mongo DB Generic Seeder Service has finished.");
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAsync<TDocument, TItem>(CancellationToken cancellationToken)
        where TDocument : class, IListDocument, new()
    {
        var collectionName = "referenceData";
        var documentId = new TDocument().Id;
        var dataTypeName = documentId?.Replace("all-", "").ToLower();
        var jsonFileName = $"{dataTypeName}.json";

        var targetJsonPath = Path.Combine(env.ContentRootPath, "Data", "Seed", jsonFileName);
        if (!File.Exists(targetJsonPath))
        {
            logger.LogInformation("Seed file '{FileName}' not found. Skipping seed for '{DocumentName}'.", jsonFileName, typeof(TDocument).Name);
            return;
        }

        var jsonString = await File.ReadAllTextAsync(targetJsonPath, cancellationToken);
        var items = JsonSerializer.Deserialize<List<TItem>>(jsonString, s_jsonSerializerOptions);

        if (items == null || items.Count == 0)
        {
            logger.LogWarning("No data found in '{FileName}'. Skipping.", jsonFileName);
            return;
        }

        var documentToSeed = new TDocument { LastUpdatedDate = DateTime.UtcNow };

        var listProperty = typeof(TDocument).GetProperties().FirstOrDefault(p => p.PropertyType == typeof(List<TItem>));
        if (listProperty == null)
        {
            logger.LogError("Could not find a List<{ItemTypeName}> property on the document type {TypeName}.", typeof(TItem).Name, typeof(TDocument).Name);
            return;
        }

        listProperty.SetValue(documentToSeed, items);

        var collection = _database.GetCollection<TDocument>(collectionName);
        var filter = Builders<TDocument>.Filter.Eq(x => x.Id, documentId);
        var options = new ReplaceOptions { IsUpsert = true };

        logger.LogInformation("Replacing '{DocumentId}' document in '{CollectionName}' collection...", documentId, collectionName);
        await collection.ReplaceOneAsync(filter, documentToSeed, options, cancellationToken);
        logger.LogInformation("Data replacement complete for '{CollectionName}'.", collectionName);
    }
}