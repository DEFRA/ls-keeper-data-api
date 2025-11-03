using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;
using System.Text.Json;

namespace KeeperData.Infrastructure.Tests.Unit.Services;

public class MongoDataSeederTests : IDisposable
{
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<MongoDataSeeder>> _mockLogger;
    private readonly Mock<IMongoClient> _mockClient;
    private readonly Mock<IMongoDatabase> _mockDatabase;
    private readonly Mock<IMongoCollection<CountryListDocument>> _mockCountryCollection;
    private readonly Mock<IMongoCollection<SpeciesListDocument>> _mockSpeciesCollection;
    private readonly Mock<IMongoCollection<RoleListDocument>> _mockRoleCollection;
    private readonly Mock<IMongoCollection<PremisesTypeListDocument>> _mockPremisesTypeCollection;
    private readonly Mock<IMongoCollection<PremisesActivityTypeListDocument>> _mockPremisesActivityTypeCollection;
    private readonly Mock<IOptions<MongoConfig>> _mockConfig;
    private readonly string _testDirectory;
    private readonly string _seedDirectory;

    public MongoDataSeederTests()
    {
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<MongoDataSeeder>>();
        _mockClient = new Mock<IMongoClient>();
        _mockDatabase = new Mock<IMongoDatabase>();
        _mockCountryCollection = new Mock<IMongoCollection<CountryListDocument>>();
        _mockSpeciesCollection = new Mock<IMongoCollection<SpeciesListDocument>>();
        _mockRoleCollection = new Mock<IMongoCollection<RoleListDocument>>();
        _mockPremisesTypeCollection = new Mock<IMongoCollection<PremisesTypeListDocument>>();
        _mockPremisesActivityTypeCollection = new Mock<IMongoCollection<PremisesActivityTypeListDocument>>();
        _mockConfig = new Mock<IOptions<MongoConfig>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), "MongoSeederTest_" + Guid.NewGuid());
        _seedDirectory = Path.Combine(_testDirectory, "Data", "Seed");
        Directory.CreateDirectory(_seedDirectory);

        _mockEnv.Setup(e => e.ContentRootPath).Returns(_testDirectory);
        _mockConfig.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "TestDb" });

        _mockDatabase.Setup(d => d.GetCollection<CountryListDocument>("refCountries", null)).Returns(_mockCountryCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<SpeciesListDocument>("refSpecies", null)).Returns(_mockSpeciesCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<RoleListDocument>("refRoles", null)).Returns(_mockRoleCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<PremisesTypeListDocument>("refPremisesTypes", null)).Returns(_mockPremisesTypeCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<PremisesActivityTypeListDocument>("refPremisesActivityTypes", null)).Returns(_mockPremisesActivityTypeCollection.Object);
        _mockClient.Setup(c => c.GetDatabase("TestDb", null)).Returns(_mockDatabase.Object);

        var lastRunField = typeof(MongoDataSeeder).GetField("_lastRun", BindingFlags.NonPublic | BindingFlags.Static);
        lastRunField?.SetValue(null, DateTime.MinValue);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
    }

    private MongoDataSeeder CreateSeeder()
    {
        return new MongoDataSeeder(_mockEnv.Object, _mockLogger.Object, _mockClient.Object, _mockConfig.Object);
    }

    private string CreateJsonFile<T>(string fileName, List<T> data)
    {
        var filePath = Path.Combine(_seedDirectory, fileName);
        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(filePath, json);
        return filePath;
    }

    private static CountryDocument CreateTestCountry(string code, string name)
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
    private static SpeciesDocument CreateTestSpecies(string code, string name)
    {
        return new SpeciesDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveEndDate = null,
            CreatedBy = "Test",
            CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastModifiedBy = null,
            LastModifiedDate = null
        };
    }

    private static RoleDocument CreateTestRole(string code, string name)
    {
        return new RoleDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveEndDate = null,
            CreatedBy = "Test",
            CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastModifiedBy = null,
            LastModifiedDate = null
        };
    }
    private static PremisesTypeDocument CreateTestPremisesType(string code, string name)
    {
        return new PremisesTypeDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static PremisesActivityTypeDocument CreateTestPremisesActivityType(string code, string name)
    {
        return new PremisesActivityTypeDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            IsActive = true,
            PriorityOrder = 0,
            EffectiveStartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public async Task StartAsync_WhenNoFilesExist_LogsAndSkipsAllDatabaseCalls()
    {
        var seeder = CreateSeeder();
        await seeder.StartAsync(CancellationToken.None);
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'countries.json' not found", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'species.json' not found", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'roles.json' not found", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'premisestypes.json' not found", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'premisesactivitytypes.json' not found", Times.Once());

        _mockCountryCollection.VerifyNoOtherCalls();
        _mockSpeciesCollection.VerifyNoOtherCalls();
        _mockRoleCollection.VerifyNoOtherCalls();
        _mockPremisesTypeCollection.VerifyNoOtherCalls();
        _mockPremisesActivityTypeCollection.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_WhenOnlyOneFileExists_SeedsOnlyThatCollection()
    {
        var seeder = CreateSeeder();

        CreateJsonFile("countries.json", [CreateTestCountry("GB", "UK")]);

        await seeder.StartAsync(CancellationToken.None);

        _mockCountryCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'species.json' not found", Times.Once());
        _mockSpeciesCollection.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_WithValidData_ReplacesAllDocuments()
    {
        var seeder = CreateSeeder();

        CreateJsonFile("countries.json", [CreateTestCountry("US", "USA")]);
        CreateJsonFile("species.json", [CreateTestSpecies("CTT", "Cattle")]);
        CreateJsonFile("roles.json", [CreateTestRole("KEEPER", "Livestock Keeper")]);
        CreateJsonFile("premisestypes.json", [CreateTestPremisesType("AH", "Agricultural Holding")]);
        CreateJsonFile("premisesactivitytypes.json", [CreateTestPremisesActivityType("AFU", "Approved Finishing Unit")]);

        CountryListDocument? capturedCountryDoc = null;
        SpeciesListDocument? capturedSpeciesDoc = null;
        RoleListDocument? capturedRoleDoc = null;
        PremisesTypeListDocument? capturedPremisesTypeDoc = null;
        PremisesActivityTypeListDocument? capturedPremisesActivityTypeDoc = null;

        _mockCountryCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                           .Callback<FilterDefinition<CountryListDocument>, CountryListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedCountryDoc = doc)
                           .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        _mockSpeciesCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<SpeciesListDocument>>(), It.IsAny<SpeciesListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                           .Callback<FilterDefinition<SpeciesListDocument>, SpeciesListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedSpeciesDoc = doc)
                           .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        _mockRoleCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<RoleListDocument>>(), It.IsAny<RoleListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                           .Callback<FilterDefinition<RoleListDocument>, RoleListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedRoleDoc = doc)
                           .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        _mockPremisesTypeCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<PremisesTypeListDocument>>(), It.IsAny<PremisesTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                   .Callback<FilterDefinition<PremisesTypeListDocument>, PremisesTypeListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedPremisesTypeDoc = doc)
                   .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        _mockPremisesActivityTypeCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<PremisesActivityTypeListDocument>>(), It.IsAny<PremisesActivityTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
                           .Callback<FilterDefinition<PremisesActivityTypeListDocument>, PremisesActivityTypeListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedPremisesActivityTypeDoc = doc)
                           .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        await seeder.StartAsync(CancellationToken.None);

        _mockCountryCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSpeciesCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<SpeciesListDocument>>(), It.IsAny<SpeciesListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRoleCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<RoleListDocument>>(), It.IsAny<RoleListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPremisesTypeCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<PremisesTypeListDocument>>(), It.IsAny<PremisesTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPremisesActivityTypeCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<PremisesActivityTypeListDocument>>(), It.IsAny<PremisesActivityTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);

        capturedCountryDoc.Should().NotBeNull();
        capturedCountryDoc!.Countries.Should().Contain(c => c.Code == "US");

        capturedSpeciesDoc.Should().NotBeNull();
        capturedSpeciesDoc!.Species.Should().Contain(s => s.Code == "CTT");

        capturedRoleDoc.Should().NotBeNull();
        capturedRoleDoc!.Roles.Should().Contain(pr => pr.Code == "KEEPER");

        capturedPremisesTypeDoc.Should().NotBeNull();
        capturedPremisesTypeDoc?.PremisesTypes.Should().Contain(pt => pt.Code == "AH");

        capturedPremisesActivityTypeDoc.Should().NotBeNull();
        capturedPremisesActivityTypeDoc?.PremisesActivityTypes.Should().Contain(pat => pat.Code == "AFU");
    }

    [Fact]
    public async Task StartAsync_WhenDbThrowsException_LogsCriticalErrorAndDoesNotThrow()
    {
        var seeder = CreateSeeder();

        CreateJsonFile("countries.json", [CreateTestCountry("US", "USA")]);

        _mockCountryCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Database connection failed"));

        await seeder.StartAsync(CancellationToken.None);

        _mockLogger.VerifyLog(LogLevel.Error, "A critical error occurred", Times.Once());
    }
}