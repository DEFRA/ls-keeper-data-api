namespace KeeperData.Api.Tests.Integration.Helpers;

using Amazon.S3.Model;
using Amazon.S3;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.LocalStack;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text;
using Amazon.SimpleNotificationService;

public class LocalStackFixture : IAsyncLifetime
{
    public LocalStackContainer LocalStackContainer { get; private set; }

    public const string TestBucket = "test-comparison-reports-bucket";

    public IAmazonSQS SqsClient { get; private set; } = null!;

    public IAmazonS3 S3Client { get; private set; } = null!;

    public IAmazonSimpleNotificationService SnsClient { get; private set; } = null!;

    public string SqsEndpoint { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists("integration-tests"); // <-- Add this line first

        LocalStackContainer = new LocalStackBuilder()
            .WithImage("localstack/localstack:2.3")
            .WithEnvironment("SERVICES", "s3,sqs,sns")
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
            .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
            .WithEnvironment("EDGE_PORT", "4566")
            .WithPortBinding(4566, true)
            .WithNetwork("integration-tests")         // Shared network name
            .WithNetworkAliases("localstack")
            .Build();     

        await LocalStackContainer.StartAsync();

        //var url = Container.GetConnectionString();
        var edgeUrl = "http://localhost:4566"; // Always use this for AWS SD

        // Create S3 client with proper LocalStack configuration
        var config = new AmazonS3Config
        {
            ServiceURL = edgeUrl,
            ForcePathStyle = true,
            UseHttp = true,
            MaxErrorRetry = 3,
            Timeout = TimeSpan.FromMinutes(5),
            //RegionEndpoint = Amazon.RegionEndpoint.EUWest2,
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
        };

        // Use consistent credentials
        S3Client = new AmazonS3Client("test", "test", config);

        // Create 'test-comparison-reports-bucket' Bucket
        var maxRetries = 5;
        var retryDelay = 3000;

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await S3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = TestBucket,
                    UseClientRegion = true
                });

                // Verify bucket creation
                var buckets = await S3Client.ListBucketsAsync();
                if (buckets.Buckets.Any(b => b.BucketName == TestBucket))
                {
                    break;
                }
            }
            catch when (i < maxRetries - 1)
            {
                await Task.Delay(retryDelay);
                retryDelay = Math.Min(retryDelay * 2, 10000); // Cap at 10 seconds
            }
        }

        SqsClient = new AmazonSQSClient("test", "test", new AmazonSQSConfig
        {
            ServiceURL = "http://localhost:4566",
            AuthenticationRegion = "eu-west-2",
            UseHttp = true
        });

        var mappedPort = LocalStackContainer.GetMappedPublicPort(4566);
        SqsEndpoint = $"http://localhost:{mappedPort}";


        // 1. Create DLQ
        var dlqName = "ls_keeper_data_intake_queue-deadletter";
        var createDlqResponse = await SqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = dlqName });
        var dlqUrl = createDlqResponse.QueueUrl;

        // 2. Get DLQ ARN
        var dlqAttributes = await SqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = dlqUrl,
            AttributeNames = new List<string> { "QueueArn" }
        });
        var dlqArn = dlqAttributes.QueueARN;

        // 3. Create main queue
        var mainQueueName = "ls_keeper_data_intake_queue";
        var createMainQueueResponse = await SqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = mainQueueName });
        var mainQueueUrl = createMainQueueResponse.QueueUrl;

        // 4. Set redrive policy
        var redrivePolicy = $"{{\"deadLetterTargetArn\":\"{dlqArn}\",\"maxReceiveCount\":\"3\"}}";
        await SqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = mainQueueUrl,
            Attributes = new Dictionary<string, string>
                    {
                        { "RedrivePolicy", redrivePolicy }
                    }
        });

        // SNS client configuration
        var snsConfig = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = edgeUrl,
            UseHttp = true,
            AuthenticationRegion = "eu-west-2"
        };
        SnsClient = new AmazonSimpleNotificationServiceClient("test", "test", snsConfig);


        // Setup shared test data
        //await SetupTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            S3Client?.Dispose();
            SqsClient?.Dispose();
            SnsClient?.Dispose();
        }
        finally
        {
            await LocalStackContainer.DisposeAsync();
        }
    }

    private async Task SetupTestDataAsync()
    {
        try
        {
            // Create simple test files first
            var testFiles = new[]
            {
                ("small.txt", "Small test content"),
                ("medium.txt", string.Join("", Enumerable.Repeat("Medium test content with more data. ", 100))),
                ("subfolder/nested.txt", "Nested file content"),
                ("test-folder/inside-folder.txt", "Content inside top-level folder"),
                ("test-folder/sub/deep.txt", "Deep nested content in folder"),
            };

            foreach (var (key, content) in testFiles)
            {
                var request = new PutObjectRequest
                {
                    BucketName = TestBucket,
                    Key = key,
                    ContentBody = content,
                    ContentType = "text/plain",
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.None
                };
                request.Metadata["test-meta"] = "test-value";

                await S3Client.PutObjectAsync(request);
            }

            // Create a large file for streaming tests
            await CreateLargeTestFileAsync("large-file.bin");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to setup test data: {ex.Message}", ex);
        }
    }

    private async Task CreateLargeTestFileAsync(string key)
    {
        try
        {
            // Create a simpler large file with just a single upload (reduced for test efficiency)
            const int fileSize = 10 * 1024 * 1024; // 10MB
            var pattern = Encoding.UTF8.GetBytes("LARGE_FILE_TEST_PATTERN_REPEATED_");
            var data = new byte[fileSize];

            // Fill data with repeating pattern
            for (var i = 0; i < fileSize; i++)
            {
                data[i] = pattern[i % pattern.Length];
            }

            var request = new PutObjectRequest
            {
                BucketName = TestBucket,
                Key = key,
                ContentType = "application/octet-stream",
                InputStream = new MemoryStream(data),
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.None
            };

            await S3Client.PutObjectAsync(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create large test file '{key}': {ex.Message}", ex);
        }
    }
}
