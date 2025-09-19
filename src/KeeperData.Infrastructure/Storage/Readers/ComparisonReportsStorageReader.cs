using Amazon.S3;
using KeeperData.Core.Storage;
using KeeperData.Infrastructure.Storage.Clients;
using KeeperData.Infrastructure.Storage.Factories;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Infrastructure.Storage.Readers;

/// <summary>
/// Wiring up the IS3ClientFactory and ComparisonReportsStorageClient into an ComparisonReportsStorageReader.
/// </summary>
/// <param name="s3ClientFactory">A single <see cref="IS3ClientFactory"/> reference.</param>
[ExcludeFromCodeCoverage]
public class ComparisonReportsStorageReader(IS3ClientFactory s3ClientFactory) : IStorageReader<ComparisonReportsStorageClient>
{
    private readonly IAmazonS3 _s3Client = s3ClientFactory.GetClient<ComparisonReportsStorageClient>();
    private readonly string _bucketName = s3ClientFactory.GetClientBucketName<ComparisonReportsStorageClient>();
}