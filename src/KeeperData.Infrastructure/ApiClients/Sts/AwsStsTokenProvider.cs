using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using KeeperData.Infrastructure.ApiClients.Configuration;
using Microsoft.AspNetCore.WebUtilities;

namespace KeeperData.Infrastructure.ApiClients.Sts;

public sealed class AwsStsTokenProvider : IStsTokenProvider
{
    private const string EmptyPayloadSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    private readonly Uri _endpointUri;
    private readonly string _region;
    private readonly int _expiresSeconds;
    private readonly Func<AWSCredentials> _credentialsFactory;
    private readonly TimeProvider _timeProvider;
    private CachedToken? _cached;

    public AwsStsTokenProvider(
        ApiClientConfiguration configuration,
        AWSCredentials? credentials = null,
        TimeProvider? timeProvider = null)
        : this(
            configuration.StsEndpoint,
            configuration.StsRegion,
            configuration.StsExpiresSeconds,
            credentials != null ? () => credentials : () => DefaultAWSCredentialsIdentityResolver.GetCredentials(),
            timeProvider)
    {
    }

    public AwsStsTokenProvider(
        string stsEndpoint,
        string stsRegion,
        int expiresSeconds = 60,
        Func<AWSCredentials>? credentialsFactory = null,
        TimeProvider? timeProvider = null)
    {
        if (!Uri.TryCreate(stsEndpoint, UriKind.Absolute, out var endpointUri)
            || (endpointUri.Scheme != Uri.UriSchemeHttps && endpointUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new ArgumentException($"Invalid STS endpoint URI '{stsEndpoint}'.", nameof(stsEndpoint));
        }

        if (string.IsNullOrWhiteSpace(stsRegion))
        {
            throw new ArgumentException("STS region is required.", nameof(stsRegion));
        }

        if (expiresSeconds <= 0 || expiresSeconds > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresSeconds), "STS presigned URL expiry must be between 1 and 300 seconds.");
        }

        _endpointUri = endpointUri;
        _region = stsRegion;
        _expiresSeconds = expiresSeconds;
        _credentialsFactory = credentialsFactory ?? (() => DefaultAWSCredentialsIdentityResolver.GetCredentials());
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var existing = Volatile.Read(ref _cached);
        if (existing is not null && now < existing.RefreshAfter)
        {
            return existing.Token;
        }

        var awsCredentials = _credentialsFactory();
        var immutable = await awsCredentials.GetCredentialsAsync().ConfigureAwait(false);

        now = _timeProvider.GetUtcNow();
        var presignedUrl = PresignGetCallerIdentity(_endpointUri, _region, immutable, now, _expiresSeconds);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(presignedUrl));

        var safetyBufferSeconds = Math.Min(15, Math.Max(1, _expiresSeconds / 4));
        var refreshAfter = now.AddSeconds(_expiresSeconds - safetyBufferSeconds);
        Volatile.Write(ref _cached, new CachedToken(encodedToken, refreshAfter));

        return encodedToken;
    }

    internal static string PresignGetCallerIdentity(
        Uri endpointUri,
        string region,
        ImmutableCredentials credentials,
        DateTimeOffset utcNow,
        int expiresSeconds)
    {
        var amzDate = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var credentialScope = $"{dateStamp}/{region}/sts/aws4_request";
        var hostHeader = endpointUri.IsDefaultPort ? endpointUri.Host : $"{endpointUri.Host}:{endpointUri.Port}";

        var queryParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Action"] = "GetCallerIdentity",
            ["Version"] = "2011-06-15",
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = $"{credentials.AccessKey}/{credentialScope}",
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = expiresSeconds.ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = "host"
        };

        if (credentials.UseToken && !string.IsNullOrEmpty(credentials.Token))
        {
            queryParams["X-Amz-Security-Token"] = credentials.Token;
        }

        var canonicalQuery = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var canonicalHeaders = $"host:{hostHeader}\n";
        var canonicalRequest = $"GET\n/\n{canonicalQuery}\n{canonicalHeaders}\nhost\n{EmptyPayloadSha256}";

        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{Sha256Hex(canonicalRequest)}";
        var signingKey = DeriveSigningKey(credentials.SecretKey, dateStamp, region, "sts");
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        return $"{endpointUri.Scheme}://{hostHeader}/?{canonicalQuery}&X-Amz-Signature={signature}";
    }

    private static string Sha256Hex(string value)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static byte[] DeriveSigningKey(string secretAccessKey, string dateStamp, string region, string service)
    {
        var kDate = HMACSHA256.HashData(Encoding.UTF8.GetBytes("AWS4" + secretAccessKey), Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HMACSHA256.HashData(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HMACSHA256.HashData(kRegion, Encoding.UTF8.GetBytes(service));
        return HMACSHA256.HashData(kService, Encoding.UTF8.GetBytes("aws4_request"));
    }

    private sealed record CachedToken(string Token, DateTimeOffset RefreshAfter);
}