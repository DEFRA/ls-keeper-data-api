using KeeperData.Api.Tests.Integration.Helpers;

namespace KeeperData.Api.Tests.Integration.Fixtures;

using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using MongoDB.Driver;
using Testcontainers.MongoDb;

public class MongoDbFixture : IAsyncLifetime
{
    public MongoDbContainer? Container { get; private set; }
    public IMongoClient MongoClient { get; private set; } = null!;
    public MongoVerifier MongoVerifier { get; private set; } = null!;

    public string? ConnectionString { get; private set; }
    private static bool s_mongoGlobalsRegistered;
    public string NetworkName { get; } = "integration-test-network";
    public const string KrdsDatabaseName = "ls-keeper-data-api";

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists(NetworkName);

        Container = new MongoDbBuilder()
            .WithImage("mongo:latest")
            .WithName("mongo")
            .WithPortBinding(50773, 27017)
            .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "testuser")
            .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "testpass")
            .WithEnvironment("MONGO_INITDB_DATABASE", "ls-keeper-data-api")
            .WithNetwork(NetworkName)
            .WithNetworkAliases("mongo")
            .Build();

        await Container.StartAsync();

        InitialiseClients();
        await VerifyResourcesAsync();
    }

    public async Task PurgeDataTables()
    {
        await Task.WhenAll([
            MongoVerifier.DeleteAll<CtsHoldingDocument>(),
            MongoVerifier.DeleteAll<CtsHoldingDocument>(),
            MongoVerifier.DeleteAll<CtsPartyDocument>(),
            MongoVerifier.DeleteAll<PartyDocument>(),
            MongoVerifier.DeleteAll<SamHerdDocument>(),
            MongoVerifier.DeleteAll<SamHoldingDocument>(),
            MongoVerifier.DeleteAll<SamPartyDocument>(),
            MongoVerifier.DeleteAll<Core.Documents.SitePartyRoleRelationshipDocument>(),
            MongoVerifier.DeleteAll<SiteDocument>()
        ]);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await MongoClient.DropDatabaseAsync(KrdsDatabaseName);
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            await Container!.DisposeAsync();
        }
    }

    private void InitialiseClients()
    {
        var mappedPort = Container!.GetMappedPublicPort(27017);
        ConnectionString = $"mongodb://testuser:testpass@localhost:{mappedPort}/{KrdsDatabaseName}?authSource=admin";

        RegisterMongoGlobals();

        MongoClient = new MongoClient(ConnectionString);
        MongoVerifier = new MongoVerifier(ConnectionString!, KrdsDatabaseName);
    }

    private async Task VerifyResourcesAsync()
    {
        var maxRetries = 5;
        var retryDelay = 1000;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await MongoClient.GetDatabase(KrdsDatabaseName)
                    .RunCommandAsync<MongoDB.Bson.BsonDocument>(
                        new MongoDB.Bson.BsonDocument("ping", 1));

                return;
            }
            catch when (i < maxRetries - 1)
            {
                await Task.Delay(retryDelay);
                retryDelay *= 2;
            }
        }

        throw new InvalidOperationException("Failed to connect to MongoDB container");
    }

    private static void RegisterMongoGlobals()
    {
        if (s_mongoGlobalsRegistered) return;

        var existing = MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer(typeof(Guid));
        if (existing is not MongoDB.Bson.Serialization.Serializers.GuidSerializer)
        {
            MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(
                typeof(Guid),
                new MongoDB.Bson.Serialization.Serializers.GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));
        }

        MongoDB.Bson.Serialization.Conventions.ConventionRegistry.Register(
            "CamelCase",
            new MongoDB.Bson.Serialization.Conventions.ConventionPack { new MongoDB.Bson.Serialization.Conventions.CamelCaseElementNameConvention() },
            _ => true
        );

        s_mongoGlobalsRegistered = true;
    }
}