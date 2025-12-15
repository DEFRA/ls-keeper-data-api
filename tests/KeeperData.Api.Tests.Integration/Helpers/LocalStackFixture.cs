namespace KeeperData.Api.Tests.Integration.Helpers;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text;
using Testcontainers.LocalStack;

public class LocalStackFixture : IAsyncLifetime
{
    public LocalStackContainer? LocalStackContainer { get; private set; }

    public const string TestBucket = "test-comparison-reports-bucket";

    public IAmazonSQS SqsClient { get; private set; } = null!;

    public IAmazonS3 S3Client { get; private set; } = null!;

    public IAmazonSimpleNotificationService SnsClient { get; private set; } = null!;
    public string NetworkName { get; } = "integration-tests";

    public string SqsEndpoint { get; private set; } = null!;
    public string LsKeeperDataIntakeQueue { get; private set; } = null!;
    public string? TopicArn { get; private set; }
    public string? ImportCompleteTopicArn { get; private set; }

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists(NetworkName); // <-- Add this line first

        LocalStackContainer = new LocalStackBuilder()
                .WithImage("localstack/localstack:latest")
                .WithName("localstack")
                .WithEnvironment("SERVICES", "s3,sqs,sns")
                .WithEnvironment("DEBUG", "1")
                .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
                .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
                .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
                .WithEnvironment("EDGE_PORT", "4566")
                .WithPortBinding(4566, 4566)
                .WithNetwork(NetworkName)
                .WithNetworkAliases("localstack")
                .Build();

        await LocalStackContainer.StartAsync();

        //TODO tidy me and remove assert
        S3Client = new AmazonS3Client("test", "test", new AmazonS3Config
        {
            ServiceURL = "http://localhost:4566",
            ForcePathStyle = true
        });
        var objectKey = "hello.txt";
        // --- Act: create bucket and upload object ---
        await S3Client.PutBucketAsync(new PutBucketRequest { BucketName = TestBucket });

        await S3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = TestBucket,
            Key = objectKey,
            ContentBody = "Hello LocalStack!"
        });

        // --- Assert: verify object exists ---
        var listResponse = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = TestBucket
        });

        Assert.Contains(listResponse.S3Objects, o => o.Key == objectKey);



        var credentials = new Amazon.Runtime.BasicAWSCredentials("test", "test");
        SqsClient = new AmazonSQSClient(credentials, new AmazonSQSConfig
        {
            ServiceURL = "http://localhost:4566",
            AuthenticationRegion = "eu-west-2",
            UseHttp = true
        });

        // SNS client
        SnsClient = new AmazonSimpleNotificationServiceClient("test", "test", new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = "http://localhost:4566",
            AuthenticationRegion = "eu-west-2",
            UseHttp = true
        });

        // 1. Create DLQ
        var dlqName = "ls_keeper_data_intake_queue-deadletter";
        await SqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = dlqName });

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
        LsKeeperDataIntakeQueue = createMainQueueResponse.QueueUrl;
        var mainQueueAttributes = await SqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = LsKeeperDataIntakeQueue,
            AttributeNames = new List<string> { "QueueArn" }
        });

        // 4. Set redrive policy
        var redrivePolicy = $"{{\"deadLetterTargetArn\":\"{dlqArn}\",\"maxReceiveCount\":\"3\"}}";
        await SqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = LsKeeperDataIntakeQueue,
            Attributes = new Dictionary<string, string>
                    {
                        { "RedrivePolicy", redrivePolicy }
                    }
        });

        var topicName = "ls-keeper-data-bridge-events";
        var createTopicResponse = await SnsClient.CreateTopicAsync(topicName);
        TopicArn = createTopicResponse.TopicArn;

        var mainQueueArn = mainQueueAttributes.QueueARN;
        //
        var importCompleteTopicName = "test-topic";
        var importCompleteCreateTopicResponse = await SnsClient.CreateTopicAsync(importCompleteTopicName);
        ImportCompleteTopicArn = importCompleteCreateTopicResponse.TopicArn;



        // 7. SQS policy for SNS
        var policy = $@"{{
                    ""Version"": ""2012-10-17"",
                    ""Statement"": [
                        {{
                            ""Effect"": ""Allow"",
                            ""Principal"": ""*"",
                            ""Action"": ""sqs:SendMessage"",
                            ""Resource"": ""{mainQueueArn}"",
                            ""Condition"": {{
                                ""ArnEquals"": {{
                                    ""aws:SourceArn"": ""{TopicArn}""
                                }}
                            }}
                        }}
                    ]
                }}";

        await SqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = LsKeeperDataIntakeQueue,
            Attributes = new Dictionary<string, string>
                    {
                        { "Policy", policy }
                    }
        });

        // 8. Subscribe SQS to SNS
        var subscribeResponse = await SnsClient.SubscribeAsync(new Amazon.SimpleNotificationService.Model.SubscribeRequest
        {
            TopicArn = TopicArn,
            Protocol = "sqs",
            Endpoint = mainQueueArn
        });

        SqsEndpoint = SqsClient.Config.ServiceURL!;

        // Setup shared test data
        //await SetupTestDataAsync();
    }

    public async Task<PublishResponse> PublishToTopicAsync(PublishRequest publishRequest, CancellationToken cancellationToken)
    {
        // SNS & SQS
        var credentials = new Amazon.Runtime.BasicAWSCredentials("test", "test");
        var amazonSimpleNotificationServiceConfig = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = SqsEndpoint,
            AuthenticationRegion = "eu-west-2",
            UseHttp = true
        };

        var _amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(credentials, amazonSimpleNotificationServiceConfig);

        //TODO not this
        return await _amazonSimpleNotificationServiceClient.PublishAsync(publishRequest, cancellationToken);
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
            await LocalStackContainer!.DisposeAsync();
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