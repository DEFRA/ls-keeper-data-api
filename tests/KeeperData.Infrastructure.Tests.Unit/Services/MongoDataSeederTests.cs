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
using NSubstitute;
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
    private readonly Mock<IMongoCollection<SiteIdentifierTypeListDocument>> _mockSiteIdentifierTypeCollection;
    private readonly Mock<IMongoCollection<ProductionUsageListDocument>> _mockProductionUsageCollection;
    private readonly Mock<IMongoCollection<FacilityBusinessActivityMapListDocument>> _mockFacilityBusinessActivityMapCollection;
    private readonly Mock<IOptions<MongoConfig>> _mockConfig;
    private readonly string _testDirectory;
    private readonly string _seedDirectory;

    private const string referenceDataCollection = "referenceData";

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
        _mockSiteIdentifierTypeCollection = new Mock<IMongoCollection<SiteIdentifierTypeListDocument>>();
        _mockProductionUsageCollection = new Mock<IMongoCollection<ProductionUsageListDocument>>();
        _mockFacilityBusinessActivityMapCollection = new Mock<IMongoCollection<FacilityBusinessActivityMapListDocument>>();
        _mockConfig = new Mock<IOptions<MongoConfig>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), "MongoSeederTest_" + Guid.NewGuid());
        _seedDirectory = Path.Combine(_testDirectory, "Data", "Seed");
        Directory.CreateDirectory(_seedDirectory);

        _mockEnv.Setup(e => e.ContentRootPath).Returns(_testDirectory);
        _mockConfig.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "TestDb" });

        _mockDatabase.Setup(d => d.GetCollection<CountryListDocument>(referenceDataCollection, null)).Returns(_mockCountryCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<SpeciesListDocument>(referenceDataCollection, null)).Returns(_mockSpeciesCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<RoleListDocument>(referenceDataCollection, null)).Returns(_mockRoleCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<PremisesTypeListDocument>(referenceDataCollection, null)).Returns(_mockPremisesTypeCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<PremisesActivityTypeListDocument>(referenceDataCollection, null)).Returns(_mockPremisesActivityTypeCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<SiteIdentifierTypeListDocument>(referenceDataCollection, null)).Returns(_mockSiteIdentifierTypeCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<ProductionUsageListDocument>(referenceDataCollection, null)).Returns(_mockProductionUsageCollection.Object);
        _mockDatabase.Setup(d => d.GetCollection<FacilityBusinessActivityMapListDocument>(referenceDataCollection, null)).Returns(_mockFacilityBusinessActivityMapCollection.Object);
        _mockClient.Setup(c => c.GetDatabase("TestDb", null)).Returns(_mockDatabase.Object);

        var lastRunField = typeof(MongoDataSeeder).GetField("s_lastRun", BindingFlags.NonPublic | BindingFlags.Static);
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

    private SiteIdentifierTypeDocument CreateTestSiteIdentifierType(string code, string name)
    {
        return new SiteIdentifierTypeDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
    }

    private ProductionUsageDocument CreateTestProductionUsage(string code, string description)
    {
        return new ProductionUsageDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            Code = code,
            Description = description,
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
    }

    private FacilityBusinessActivityMapDocument CreateTestFacilityBusinessActivityMap(string facilityActivityCode, string associatedPremiseTypeCode, string associatedPremiseActivityCode)
    {
        return new FacilityBusinessActivityMapDocument
        {
            IdentifierId = Guid.NewGuid().ToString(),
            FacilityActivityCode = facilityActivityCode,
            AssociatedPremiseTypeCode = associatedPremiseTypeCode,
            AssociatedPremiseActivityCode = associatedPremiseActivityCode,
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
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
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'siteidentifiertypes.json' not found", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'productionusages.json' not found", Times.Once());
        _mockLogger.VerifyLog(LogLevel.Information, "Seed file 'facilitybusinessactivitymaps.json' not found", Times.Once());

        _mockCountryCollection.VerifyNoOtherCalls();
        _mockSpeciesCollection.VerifyNoOtherCalls();
        _mockRoleCollection.VerifyNoOtherCalls();
        _mockPremisesTypeCollection.VerifyNoOtherCalls();
        _mockPremisesActivityTypeCollection.VerifyNoOtherCalls();
        _mockSiteIdentifierTypeCollection.VerifyNoOtherCalls();
        _mockProductionUsageCollection.VerifyNoOtherCalls();
        _mockFacilityBusinessActivityMapCollection.VerifyNoOtherCalls();
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

        CreateJsonFile("countries.json", new List<CountryDocument> { CreateTestCountry("US", "USA") });
        CreateJsonFile("species.json", new List<SpeciesDocument> { CreateTestSpecies("CTT", "Cattle") });
        CreateJsonFile("roles.json", new List<RoleDocument> { CreateTestRole("KEEPER", "Livestock Keeper") });
        CreateJsonFile("premisestypes.json", new List<PremisesTypeDocument> { CreateTestPremisesType("AH", "Agricultural Holding") });
        CreateJsonFile("premisesactivitytypes.json", new List<PremisesActivityTypeDocument> { CreateTestPremisesActivityType("AFU", "Approved Finishing Unit") });
        CreateJsonFile("siteidentifiertypes.json", new List<SiteIdentifierTypeDocument> { CreateTestSiteIdentifierType("CPHN", "CPH Number") });
        CreateJsonFile("productionusages.json", new List<ProductionUsageDocument> { CreateTestProductionUsage("BEEF", "Beef") });
        CreateJsonFile("facilitybusinessactivitymaps.json", new List<FacilityBusinessActivityMapDocument> { CreateTestFacilityBusinessActivityMap("XX-XX-XX", "YY-YY", "ZZ") });

        CountryListDocument? capturedCountryDoc = null;
        SpeciesListDocument? capturedSpeciesDoc = null;
        RoleListDocument? capturedRoleDoc = null;
        PremisesTypeListDocument? capturedPremisesTypeDoc = null;
        PremisesActivityTypeListDocument? capturedPremisesActivityTypeDoc = null;
        SiteIdentifierTypeListDocument? capturedSiteIdentifierTypeDoc = null;
        ProductionUsageListDocument? capturedProductionUsageDoc = null;
        FacilityBusinessActivityMapListDocument? capturedFacilityBusinessActivityMapDoc = null;

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

        _mockSiteIdentifierTypeCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<SiteIdentifierTypeListDocument>>(), It.IsAny<SiteIdentifierTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<SiteIdentifierTypeListDocument>, SiteIdentifierTypeListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedSiteIdentifierTypeDoc = doc)
            .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        _mockProductionUsageCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<ProductionUsageListDocument>>(), It.IsAny<ProductionUsageListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<ProductionUsageListDocument>, ProductionUsageListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedProductionUsageDoc = doc)
            .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        _mockFacilityBusinessActivityMapCollection.Setup(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<FacilityBusinessActivityMapListDocument>>(), It.IsAny<FacilityBusinessActivityMapListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()))
           .Callback<FilterDefinition<FacilityBusinessActivityMapListDocument>, FacilityBusinessActivityMapListDocument, ReplaceOptions, CancellationToken>((_, doc, _, _) => capturedFacilityBusinessActivityMapDoc = doc)
           .Returns(Task.FromResult(Mock.Of<ReplaceOneResult>()));

        await seeder.StartAsync(CancellationToken.None);

        _mockCountryCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<CountryListDocument>>(), It.IsAny<CountryListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSpeciesCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<SpeciesListDocument>>(), It.IsAny<SpeciesListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRoleCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<RoleListDocument>>(), It.IsAny<RoleListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPremisesTypeCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<PremisesTypeListDocument>>(), It.IsAny<PremisesTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPremisesActivityTypeCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<PremisesActivityTypeListDocument>>(), It.IsAny<PremisesActivityTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSiteIdentifierTypeCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<SiteIdentifierTypeListDocument>>(), It.IsAny<SiteIdentifierTypeListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockFacilityBusinessActivityMapCollection.Verify(x => x.ReplaceOneAsync(It.IsAny<FilterDefinition<FacilityBusinessActivityMapListDocument>>(), It.IsAny<FacilityBusinessActivityMapListDocument>(), It.IsAny<ReplaceOptions>(), It.IsAny<CancellationToken>()), Times.Once);

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

        capturedSiteIdentifierTypeDoc.Should().NotBeNull();
        capturedSiteIdentifierTypeDoc?.SiteIdentifierTypes.Should().Contain(sit => sit.Code == "CPHN");

        capturedProductionUsageDoc.Should().NotBeNull();
        capturedProductionUsageDoc?.ProductionUsages.Should().Contain(pu => pu.Code == "BEEF");

        capturedFacilityBusinessActivityMapDoc.Should().NotBeNull();
        capturedFacilityBusinessActivityMapDoc?.FacilityBusinessActivityMaps.Should().Contain(pu => pu.FacilityActivityCode == "XX-XX-XX");
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