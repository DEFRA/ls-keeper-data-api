using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Core.Storage.Sqlite;
using KeeperData.Infrastructure.Database.Repositories;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Storage.Configuration;
using KeeperData.Infrastructure.Storage.Sources;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace KeeperData.Api.Tests.Integration.Services;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CphSqliteCacheIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly LocalStackFixture _localStackFixture;
    private ServiceProvider? _serviceProvider;

    private const string TestBucket = "cph-sqlite-test-bucket";
    private const string S3Prefix = "views/";

    private string _cachePath = null!;
    private string _sqliteFilePath = null!;
    private readonly List<string> _testCphs =
    [
        "01/001/0001", "01/002/0002", "02/001/0001", "02/003/0003",
        "03/001/0001", "03/002/0002", "04/001/0001", "05/001/0001",
        "06/001/0001", "07/001/0001", "08/001/0001", "09/001/0001",
        "10/001/0001", "11/001/0001", "12/001/0001"
    ];

    public CphSqliteCacheIntegrationTests(
        ITestOutputHelper output,
        LocalStackFixture localStackFixture)
    {
        _output = output;
        _localStackFixture = localStackFixture;
    }

    public async Task InitializeAsync()
    {
        _cachePath = Path.Combine(Path.GetTempPath(), $"cph_cache_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_cachePath);

        await CreateTestBucketAsync();
        _sqliteFilePath = CreateTestSqliteFile("cphs_20260630T120000Z.sqlite");
        await UploadSqliteToS3Async(_sqliteFilePath, $"{S3Prefix}cphs_20260630T120000Z.sqlite");

        _serviceProvider = BuildServices();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
            await _serviceProvider.DisposeAsync();

        try { Directory.Delete(_cachePath, recursive: true); } catch { }
        try { if (File.Exists(_sqliteFilePath)) File.Delete(_sqliteFilePath); } catch { }

        try
        {
            var objects = await _localStackFixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = TestBucket
            });
            foreach (var obj in objects.S3Objects)
            {
                await _localStackFixture.S3Client.DeleteObjectAsync(TestBucket, obj.Key);
            }
        }
        catch { }
    }

    [Fact]
    public async Task CacheService_StartAsync_ShouldDownloadAndCacheSqlite()
    {
        _output.WriteLine("=== Cache Service Download Test ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();

        cacheService.IsLoaded.Should().BeFalse("cache should not be loaded before StartAsync");

        await cacheService.StartAsync(CancellationToken.None);

        cacheService.IsLoaded.Should().BeTrue("cache should be loaded after StartAsync");
        cacheService.CachedFileName.Should().Be("cphs_20260630T120000Z.sqlite");
        cacheService.DataTimestamp.Should().NotBeNull();
        cacheService.LastRefreshedAt.Should().NotBeNull();
        cacheService.GetCurrentDbPath().Should().NotBeNull();
        File.Exists(cacheService.GetCurrentDbPath()!).Should().BeTrue("cached file should exist on disk");

        _output.WriteLine($"Cached file: {cacheService.CachedFileName}");
        _output.WriteLine($"Data timestamp: {cacheService.DataTimestamp:o}");
        _output.WriteLine($"DB path: {cacheService.GetCurrentDbPath()}");

        await cacheService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CacheService_RefreshAsync_ShouldSkipWhenSameFile()
    {
        _output.WriteLine("=== Cache Service Skip-Same-File Test ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();

        await cacheService.StartAsync(CancellationToken.None);
        var firstPath = cacheService.GetCurrentDbPath();
        var firstRefreshTime = cacheService.LastRefreshedAt;

        await Task.Delay(100);

        await cacheService.RefreshCacheAsync(CancellationToken.None);

        cacheService.GetCurrentDbPath().Should().Be(firstPath, "path should not change for same file");
        cacheService.LastRefreshedAt.Should().BeAfter(firstRefreshTime!.Value,
            "LastRefreshedAt should update even when file is same");

        _output.WriteLine("Verified cache skip for same file");

        await cacheService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CacheService_RefreshAsync_ShouldSwapWhenNewerFileAppears()
    {
        _output.WriteLine("=== Cache Service Hot-Swap Test ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();

        await cacheService.StartAsync(CancellationToken.None);
        var originalFileName = cacheService.CachedFileName;
        _output.WriteLine($"Initial file: {originalFileName}");

        var newerSqlite = CreateTestSqliteFile("cphs_20260701T060000Z.sqlite",
            ["99/001/0001", "99/002/0002", "99/003/0003"]);
        await UploadSqliteToS3Async(newerSqlite, $"{S3Prefix}cphs_20260701T060000Z.sqlite");
        _output.WriteLine("Uploaded newer SQLite file to S3");

        await cacheService.RefreshCacheAsync(CancellationToken.None);

        cacheService.CachedFileName.Should().Be("cphs_20260701T060000Z.sqlite",
            "should swap to newer file");
        cacheService.IsLoaded.Should().BeTrue();

        _output.WriteLine($"Swapped to: {cacheService.CachedFileName}");

        try { File.Delete(newerSqlite); } catch { }
        await cacheService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Repository_GetPagedAsync_ShouldReturnDataFromCache()
    {
        _output.WriteLine("=== Repository Paged Query Test ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();
        await cacheService.StartAsync(CancellationToken.None);

        var repository = _serviceProvider!.GetRequiredService<ICphRepository>();

        var (items, totalCount) = await repository.GetPagedAsync(1, 10, null);

        totalCount.Should().Be(_testCphs.Count, $"should have {_testCphs.Count} total CPHs");
        items.Should().HaveCount(10, "page size 10, first page");
        items.Should().AllSatisfy(i => i.Cph.Should().NotBeNullOrEmpty());

        _output.WriteLine($"Total: {totalCount}, Page 1: {items.Count} items");

        await cacheService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Repository_GetPagedAsync_ShouldSupportPagination()
    {
        _output.WriteLine("=== Repository Pagination Test ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();
        await cacheService.StartAsync(CancellationToken.None);

        var repository = _serviceProvider!.GetRequiredService<ICphRepository>();

        var (page1, total1) = await repository.GetPagedAsync(1, 5, null);
        var (page2, total2) = await repository.GetPagedAsync(2, 5, null);
        var (page3, total3) = await repository.GetPagedAsync(3, 5, null);

        total1.Should().Be(_testCphs.Count);
        total2.Should().Be(_testCphs.Count);
        total3.Should().Be(_testCphs.Count);

        page1.Should().HaveCount(5);
        page2.Should().HaveCount(5);
        page3.Should().HaveCount(5);

        var allCphs = page1.Concat(page2).Concat(page3).Select(c => c.Cph).ToList();
        allCphs.Should().OnlyHaveUniqueItems("pages should not overlap");
        allCphs.Should().HaveCount(_testCphs.Count);

        _output.WriteLine($"Pages: [{page1.Count}] [{page2.Count}] [{page3.Count}] = {allCphs.Count} unique");

        await cacheService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Repository_GetPagedAsync_ShouldSupportSortDesc()
    {
        _output.WriteLine("=== Repository Sort Desc Test ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();
        await cacheService.StartAsync(CancellationToken.None);

        var repository = _serviceProvider!.GetRequiredService<ICphRepository>();

        var (ascending, _) = await repository.GetPagedAsync(1, _testCphs.Count, "asc");
        var (descending, _) = await repository.GetPagedAsync(1, _testCphs.Count, "desc");

        ascending.First().Cph.Should().Be(_testCphs.Order().First());
        descending.First().Cph.Should().Be(_testCphs.OrderDescending().First());

        ascending.Select(c => c.Cph).Should().BeInAscendingOrder();
        descending.Select(c => c.Cph).Should().BeInDescendingOrder();

        _output.WriteLine($"Asc first: {ascending.First().Cph}, Desc first: {descending.First().Cph}");

        await cacheService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Repository_GetPagedAsync_ShouldReturnEmptyWhenCacheNotLoaded()
    {
        _output.WriteLine("=== Repository Empty When Not Loaded Test ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();
        cacheService.IsLoaded.Should().BeFalse();

        var repository = _serviceProvider!.GetRequiredService<ICphRepository>();

        var (items, totalCount) = await repository.GetPagedAsync(1, 10, null);

        items.Should().BeEmpty();
        totalCount.Should().Be(0);

        _output.WriteLine("Verified empty results when cache not loaded");
    }

    [Fact]
    public async Task CacheService_ShouldHandleEmptyS3Bucket()
    {
        _output.WriteLine("=== Cache Service Empty S3 Test ===");

        var objects = await _localStackFixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = TestBucket,
            Prefix = S3Prefix
        });
        foreach (var obj in objects.S3Objects)
        {
            await _localStackFixture.S3Client.DeleteObjectAsync(TestBucket, obj.Key);
        }

        var emptyServices = BuildServices();
        var cacheService = emptyServices.GetRequiredService<CphSqliteCacheService>();

        await cacheService.StartAsync(CancellationToken.None);

        cacheService.IsLoaded.Should().BeFalse("should not load when no files in S3");
        cacheService.GetCurrentDbPath().Should().BeNull();

        _output.WriteLine("Verified graceful handling of empty S3");

        await cacheService.StopAsync(CancellationToken.None);
        await emptyServices.DisposeAsync();

        await UploadSqliteToS3Async(_sqliteFilePath, $"{S3Prefix}cphs_20260630T120000Z.sqlite");
    }

    [Fact]
    public async Task FullFlow_CacheLoadThenQueryThroughRepository()
    {
        _output.WriteLine("=== Full E2E Flow: S3 -> Cache -> Repository -> EF Core ===");

        var cacheService = _serviceProvider!.GetRequiredService<CphSqliteCacheService>();

        cacheService.IsLoaded.Should().BeFalse();
        _output.WriteLine("Step 1: Cache not loaded");

        await cacheService.StartAsync(CancellationToken.None);
        cacheService.IsLoaded.Should().BeTrue();
        _output.WriteLine($"Step 2: Cache loaded — {cacheService.CachedFileName}");

        var repository = _serviceProvider!.GetRequiredService<ICphRepository>();

        var (firstPage, total) = await repository.GetPagedAsync(1, 5, "asc");
        total.Should().Be(_testCphs.Count);
        firstPage.Should().HaveCount(5);
        firstPage.Select(c => c.Cph).Should().BeInAscendingOrder();
        _output.WriteLine($"Step 3: Queried page 1 — {firstPage.Count}/{total} CPHs");

        var (lastPage, _) = await repository.GetPagedAsync(3, 5, "asc");
        lastPage.Should().HaveCount(5);
        _output.WriteLine($"Step 4: Queried last page — {lastPage.Count} CPHs");

        var allCphs = new List<string>();
        for (var page = 1; page <= 3; page++)
        {
            var (items, _) = await repository.GetPagedAsync(page, 5, "asc");
            allCphs.AddRange(items.Select(c => c.Cph));
        }
        allCphs.Should().BeEquivalentTo(_testCphs);
        _output.WriteLine($"Step 5: All {allCphs.Count} CPHs verified across pages");

        _output.WriteLine("=== Full E2E Flow PASSED ===");

        await cacheService.StopAsync(CancellationToken.None);
    }

    private async Task CreateTestBucketAsync()
    {
        try
        {
            await _localStackFixture.S3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = TestBucket
            });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou")
        {
        }
    }

    private string CreateTestSqliteFile(string fileName, List<string>? cphs = null)
    {
        cphs ??= _testCphs;
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        if (File.Exists(filePath)) File.Delete(filePath);

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = "CREATE TABLE cphs (cph TEXT NOT NULL)";
        createCmd.ExecuteNonQuery();

        using var transaction = connection.BeginTransaction();
        using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO cphs (cph) VALUES ($cph)";
        var param = insertCmd.Parameters.Add("$cph", SqliteType.Text);

        foreach (var cph in cphs)
        {
            param.Value = cph;
            insertCmd.ExecuteNonQuery();
        }

        transaction.Commit();
        connection.Close();
        SqliteConnection.ClearPool(connection);

        return filePath;
    }

    private async Task UploadSqliteToS3Async(string localPath, string s3Key)
    {
        await using var stream = File.OpenRead(localPath);
        await _localStackFixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = TestBucket,
            Key = s3Key,
            InputStream = stream,
            ContentType = "application/x-sqlite3"
        });
    }

    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddHttpClient(
                DataBridgeSqliteArtifactSource.MetadataClientName,
                client => client.BaseAddress = new Uri("https://data-bridge.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new LatestSqliteStubHandler(GetLatestPresignedArtifactAsync));

        services.AddHttpClient(DataBridgeSqliteArtifactSource.DownloadClientName);

        services.AddSingleton<ISqliteArtifactSource, DataBridgeSqliteArtifactSource>();

        var cacheConfig = new CphSqliteCacheConfiguration
        {
            Enabled = true,
            CachePath = _cachePath,
            FilePattern = "cphs_",
            LatestArtifactRoute = DataBridgeApiRoutes.GetLatestCphSqlite,
            RefreshIntervalHours = 24
        };
        services.AddSingleton(cacheConfig);

        services.AddSingleton<CphSqliteCacheService>();
        services.AddSingleton<ICphSqliteCacheService>(sp => sp.GetRequiredService<CphSqliteCacheService>());
        services.AddScoped<ICphRepository, CphRepository>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Stands in for the data bridge, presigning the newest CPH database in the bucket exactly as
    /// GET api/etl/sqlite/cphs/latest does.
    /// </summary>
    private async Task<SqliteArtifactLatestResponse?> GetLatestPresignedArtifactAsync()
    {
        var objects = await _localStackFixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = TestBucket,
            Prefix = S3Prefix
        });

        var latest = objects.S3Objects
            .Where(o => o.Key.Split('/').Last().StartsWith("cphs_", StringComparison.Ordinal)
                        && o.Key.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.Key, StringComparer.Ordinal)
            .FirstOrDefault();

        if (latest is null)
        {
            return null;
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var downloadUrl = await _localStackFixture.S3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = TestBucket,
            Key = latest.Key,
            Verb = HttpVerb.GET,
            Expires = expiresAt
        });

        return new SqliteArtifactLatestResponse
        {
            ObjectKey = latest.Key,
            DownloadUrl = downloadUrl,
            Size = latest.Size ?? 0,
            LastModified = latest.LastModified ?? DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    private sealed class LatestSqliteStubHandler(Func<Task<SqliteArtifactLatestResponse?>> resolveLatest)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var artifact = await resolveLatest();

            if (artifact is null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(artifact), Encoding.UTF8, "application/json")
            };
        }
    }
}