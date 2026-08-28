using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Storage.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace KeeperData.Infrastructure.Storage.Sources;

/// <summary>
/// Asks the data bridge for the latest published SQLite database and downloads it from the presigned
/// URL it hands back. The bridge owns the bucket holding those files, so this keeps the API free of
/// any direct access to it.
/// </summary>
public class DataBridgeSqliteArtifactSource : ISqliteArtifactSource
{
    /// <summary>The bridge client, carrying the API subscription key and retry policy.</summary>
    public const string MetadataClientName = "DataBridgeApi";

    /// <summary>
    /// The download client. Presigned URLs are signed for a fixed set of headers, so the download
    /// must not travel on the client that attaches the bridge credentials.
    /// </summary>
    public const string DownloadClientName = "SqliteArtifactDownload";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DataBridgeSqliteArtifactSource> _logger;
    private readonly string? _serviceName;

    public DataBridgeSqliteArtifactSource(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DataBridgeSqliteArtifactSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serviceName = configuration.GetValue<string>("ApiClients:DataBridgeApi:ServiceName");
    }

    public async Task<SqliteArtifact?> GetLatestAsync(string latestArtifactRoute, CancellationToken cancellationToken)
    {
        var requestUri = string.IsNullOrWhiteSpace(_serviceName)
            ? latestArtifactRoute
            : $"{_serviceName}/{latestArtifactRoute}";

        var client = _httpClientFactory.CreateClient(MetadataClientName);

        using var response = await client.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Data bridge holds no SQLite artifact for {Route}", latestArtifactRoute);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SqliteArtifactLatestResponse>(
            stream, SerializerOptions, cancellationToken);

        if (payload is null || string.IsNullOrWhiteSpace(payload.ObjectKey) || string.IsNullOrWhiteSpace(payload.DownloadUrl))
        {
            _logger.LogWarning("Data bridge returned an incomplete SQLite artifact for {Route}", latestArtifactRoute);
            return null;
        }

        _logger.LogInformation(
            "Data bridge reports latest SQLite artifact {ObjectKey}, size: {Size}, last modified: {LastModified:o}",
            payload.ObjectKey, payload.Size, payload.LastModified);

        return new SqliteArtifact
        {
            ObjectKey = payload.ObjectKey,
            DownloadUrl = payload.DownloadUrl,
            Size = payload.Size,
            LastModified = payload.LastModified,
            ExpiresAt = payload.ExpiresAt
        };
    }

    public async Task DownloadAsync(SqliteArtifact artifact, string localPath, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(DownloadClientName);

        using var response = await client.GetAsync(
            artifact.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(localPath);
        await contentStream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Downloaded SQLite artifact {ObjectKey} to {LocalPath}", artifact.ObjectKey, localPath);
    }
}