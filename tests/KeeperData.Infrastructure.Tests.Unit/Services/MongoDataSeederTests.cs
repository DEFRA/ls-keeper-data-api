using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Services;

public class MongoDataSeederTests : IDisposable
{
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<MongoDataSeeder>> _mockLogger;
    private readonly Mock<IMongoClient> _mockClient;
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<CountryListDocument>> _mockCollection;
    private readonly Mock<IOptions<MongoConfig>> _mockConfig;
    private readonly string _testDirectory;
    private readonly string _seedDirectory;

    public MongoDataSeederTests()
    {
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<MongoDataSeeder>>();
        _mockClient = new Mock<IMongoClient>();
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<CountryListDocument>>();
        _mockConfig = new Mock<IOptions<MongoConfig>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), "MongoSeederTest_" + Guid.NewGuid());
        _seedDirectory = Path.Combine(_testDirectory, "Data", "Seed");
        Directory.CreateDirectory(_seedDirectory);

        _mockEnv.Setup(e => e.ContentRootPath).Returns(_testDirectory);
        _mockConfig.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "TestDb" });
        _mockDatabase.Setup(d => d.GetCollection<CountryListDocument>(It.IsAny<string>(), null))
                     .Returns(_mockCollection.Object);
        _mockClient.Setup(c => c.GetDatabase("TestDb", null)).Returns(_mockDatabase.Object);

        var collectionNamespace = new CollectionNamespace("TestDb", "refCountries");
        _mockCollection.Setup(c => c.CollectionNamespace).Returns(collectionNamespace);

        var lastRunField = typeof(MongoDataSeeder).GetField("_lastRun", BindingFlags.NonPublic | BindingFlags.Static);
        lastRunField?.SetValue(null, DateTime.MinValue);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    private MongoDataSeeder CreateSeeder()
    {
        return new MongoDataSeeder(
            _mockEnv.Object,
            _mockLogger.Object,
            _mockClient.Object,
            _mockConfig.Object);
    }

    private CountryDocument CreateTestCountry(string code, string name)
    {
        return new CountryDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            LongName = name,
            IsActive = true,
            EuTradeMember = false,
            DevolvedAuthority = false,
            SortOrder = 0,
            EffectiveStartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveEndDate = null,
            CreatedBy = "Test",
            CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastModifiedBy = null,
            LastModifiedDate = null
        };
    }

    [Fact]
    public async Task StartAsync_WhenSeedFileDoesNotExist_LogsWarningAndDoesNotWriteToDb()
    {
        // Arrange
        var seeder = CreateSeeder();

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.VerifyLog(LogLevel.Warning, "Seed file 'countries.json' not found");
        _mockCollection.Verify(
            x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenSeedFileIsEmpty_LogsWarningAndDoesNotWriteToDb()
    {
        // Arrange
        var seeder = CreateSeeder();
        var seedFile = Path.Combine(_seedDirectory, "countries.json");
        await File.WriteAllTextAsync(seedFile, "[]");

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.VerifyLog(LogLevel.Warning, "No data found");
        _mockCollection.Verify(
            x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithValidData_ReplacesDocumentInCollection()
    {
        // Arrange
        var seeder = CreateSeeder();

        var countries = new List<CountryDocument>
        {
            CreateTestCountry("US", "United States"),
            CreateTestCountry("CA", "Canada")
        };
        var seedFile = Path.Combine(_seedDirectory, "countries.json");
        await File.WriteAllTextAsync(seedFile, JsonSerializer.Serialize(countries));

        CountryListDocument capturedDocument = null;
        _mockCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                       .Callback<FilterDefinition<CountryListDocument>, CountryListDocument, ReplaceOptions, CancellationToken>((filter, doc, opts, token) => capturedDocument = doc)
                       .Returns(Task.FromResult<ReplaceOneResult>(null)); // Use null result for abstract type

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        _mockCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.Is<ReplaceOptions>(o => o.IsUpsert), It.IsAny<CancellationToken>()), Times.Once);

        capturedDocument.Should().NotBeNull();
        capturedDocument.Id.Should().Be("all-countries");
        capturedDocument.Countries.Should().HaveCount(2);
        capturedDocument.Countries.Should().Contain(c => c.Code == "US");
        capturedDocument.Countries.Should().Contain(c => c.Code == "CA");
    }

    [Fact]
    public async Task StartAsync_WhenExceptionOccurs_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        var seeder = CreateSeeder();
        var countries = new List<CountryDocument> { CreateTestCountry("US", "United States") };
        var seedFile = Path.Combine(_seedDirectory, "countries.json");
        await File.WriteAllTextAsync(seedFile, JsonSerializer.Serialize(countries));

        _mockCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new MongoException("Database connection failed"));

        // Act
        await seeder.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.VerifyLog(LogLevel.Error, "An error occurred while seeding");
    }
}

public static class LoggerMockExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel level, string message)
    {
        mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}