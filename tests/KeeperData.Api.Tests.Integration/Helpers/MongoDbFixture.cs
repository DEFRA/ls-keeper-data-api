namespace KeeperData.Api.Tests.Integration.Helpers;

using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

public class MongoDbFixture : IAsyncLifetime
{
    public MongoDbContainer Container { get; private set; }

    public IMongoClient MongoClient { get; private set; } = null!;

    public MongoVerifier MongoVerifier { get; private set; } = null!;

    public string ConnectionString { get; private set; }

    public const string TestDatabaseName = "ls-keeper-data-api";

    private static bool s_mongoGlobalsRegistered;

    public string NetworkName { get; } = "integration-tests";

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists("integration-tests"); // <-- Add this line first

        Container = new MongoDbBuilder()
              .WithImage("mongo:latest")
              .WithName("mongo")
              .WithPortBinding(27017, true) // dynamic host port
              .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "testuser")
              .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "testpass")
              .WithEnvironment("MONGO_INITDB_DATABASE", "ls-keeper-data-api")
              .WithNetwork(NetworkName)
              .WithNetworkAliases("mongo")
              .Build();

        await Container.StartAsync();

        // Retrieve the connection string after the container has started
        var mappedPort = Container.GetMappedPublicPort(27017);
        ConnectionString = $"mongodb://testuser:testpass@localhost:{mappedPort}/{TestDatabaseName}?authSource=admin";

        MongoClient = new MongoClient(ConnectionString);
        var database = MongoClient.GetDatabase(TestDatabaseName);
      
        // Verify connection
        await VerifyConnectionAsync();

        MongoVerifier = new MongoVerifier(ConnectionString, MongoDbFixture.TestDatabaseName);
    }

    public async Task DisposeAsync()
    {
        try
        {
            RegisterMongoGlobals();

            // Clean up database before disposing
            await MongoClient.DropDatabaseAsync(TestDatabaseName);
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            await Container.DisposeAsync();
        }
    }

    private async Task VerifyConnectionAsync()
    {
        var maxRetries = 5;
        var retryDelay = 1000;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await MongoClient.GetDatabase(TestDatabaseName)
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
