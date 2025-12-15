using KeeperData.Api.Tests.Integration.Helpers;

namespace KeeperData.Api.Tests.Integration.Fixtures;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Threading;
using Testcontainers.LocalStack;

public class LocalStackFixture : IAsyncLifetime
{
    public LocalStackContainer? LocalStackContainer { get; private set; }

    public IAmazonSQS SqsClient { get; private set; } = null!;
    public IAmazonS3 S3Client { get; private set; } = null!;
    public IAmazonSimpleNotificationService SnsClient { get; private set; } = null!;
    public static Amazon.Runtime.BasicAWSCredentials GetBasicAWSCredentials => new("test", "test");

    public string? SqsEndpoint { get; private set; }
    public string? KrdsIntakeQueueUrl { get; private set; }
    public string? DataBridgeEventsTopicArn { get; private set; }
    public string? KrdsImportCompleteTopicArn { get; private set; }

    public const string ServiceURL = "http://localhost:4566";
    public const string AuthenticationRegion = "eu-west-2";
    public const string NetworkName = "integration-test-network";

    public const string S3_Bucket_TestComparisonReportsBucket = "test-comparison-reports-bucket";
    public const string SQS_IntakeQueue = "ls_keeper_data_intake_queue";
    public const string SQS_IntakeDeadletterQueue = "ls_keeper_data_intake_queue-deadletter";
    public const string SNS_DataBridgeEventsTopic = "ls-keeper-data-bridge-events";
    public const string SNS_KrdsImportCompleteTopic = "ls_keeper_data_import_complete";

    public async Task InitializeAsync()
    {
        DockerNetworkHelper.EnsureNetworkExists(NetworkName);

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

        InitialiseClients();
        await InitialiseResourcesAsync();
        await VerifyResourcesAsync();
    }

    public async Task<PublishResponse> PublishToTopicAsync(PublishRequest publishRequest, CancellationToken cancellationToken)
    {
        var _amazonSimpleNotificationServiceClient = new AmazonSimpleNotificationServiceClient(
            GetBasicAWSCredentials,
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = SqsEndpoint,
                AuthenticationRegion = AuthenticationRegion,
                UseHttp = true
            });

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

    private void InitialiseClients()
    {
        // S3
        S3Client = new AmazonS3Client("test", "test", new AmazonS3Config
        {
            ServiceURL = ServiceURL,
            ForcePathStyle = true
        });

        // SQS
        SqsClient = new AmazonSQSClient(GetBasicAWSCredentials, new AmazonSQSConfig
        {
            ServiceURL = ServiceURL,
            AuthenticationRegion = AuthenticationRegion,
            UseHttp = true
        });

        SqsEndpoint = SqsClient.Config.ServiceURL!;

        // SNS
        SnsClient = new AmazonSimpleNotificationServiceClient(GetBasicAWSCredentials, new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = ServiceURL,
            AuthenticationRegion = AuthenticationRegion,
            UseHttp = true
        });
    }

    private async Task InitialiseResourcesAsync()
    {
        // S3
        // S3: Create comparison reports bucket
        await S3Client.PutBucketAsync(new PutBucketRequest { BucketName = S3_Bucket_TestComparisonReportsBucket });

        // SQS
        // Create intake DLQ queue
        var intakeDlqCreated = await SqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = SQS_IntakeDeadletterQueue });
        var intakeDlqAttr = await SqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = intakeDlqCreated.QueueUrl,
            AttributeNames = ["QueueArn"]
        });

        // Create intake queue
        var intakeQueueCreated = await SqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = SQS_IntakeQueue });
        KrdsIntakeQueueUrl = intakeQueueCreated.QueueUrl;

        var intakeQueueAttr = await SqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = KrdsIntakeQueueUrl,
            AttributeNames = ["QueueArn"]
        });

        // Set redrive policy for intake DLQ
        var redrivePolicy = $"{{\"deadLetterTargetArn\":\"{intakeDlqAttr.QueueARN}\",\"maxReceiveCount\":\"3\"}}";
        await SqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = KrdsIntakeQueueUrl,
            Attributes = new Dictionary<string, string>
            {
                { "RedrivePolicy", redrivePolicy }
            }
        });

        // SNS
        // SNS: Create data bridge events topic
        var bridgeEventsTopicCreated = await SnsClient.CreateTopicAsync(SNS_DataBridgeEventsTopic);
        DataBridgeEventsTopicArn = bridgeEventsTopicCreated.TopicArn;

        // SNS: Create import completed topic
        var importCompleteTopicCreated = await SnsClient.CreateTopicAsync(SNS_KrdsImportCompleteTopic);
        KrdsImportCompleteTopicArn = importCompleteTopicCreated.TopicArn;

        // SNS: Add subscription for data bridge events topic to intake queue
        var intakeQueuePolicy = $@"{{
            ""Version"": ""2012-10-17"",
            ""Statement"": [
                {{
                    ""Effect"": ""Allow"",
                    ""Principal"": ""*"",
                    ""Action"": ""sqs:SendMessage"",
                    ""Resource"": ""{intakeQueueAttr.QueueARN}"",
                    ""Condition"": {{
                        ""ArnEquals"": {{
                            ""aws:SourceArn"": ""{DataBridgeEventsTopicArn}""
                        }}
                    }}
                }}
            ]
        }}";

        await SqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = KrdsIntakeQueueUrl,
            Attributes = new Dictionary<string, string>
            {
                { "Policy", intakeQueuePolicy }
            }
        });

        await SnsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = DataBridgeEventsTopicArn,
            Endpoint = intakeQueueAttr.QueueARN,
            Protocol = "sqs"
        });
    }

    private async Task VerifyResourcesAsync()
    {
        // S3
        // S3: comparison reports bucket
        await S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = S3_Bucket_TestComparisonReportsBucket
        });

        // SQS
        // SQS: Intake DLQ queue
        await SqsClient.GetQueueAttributesAsync(SQS_IntakeDeadletterQueue, ["All"], CancellationToken.None);

        // SQS: Intake queue
        await SqsClient.GetQueueAttributesAsync(SQS_IntakeQueue, ["All"], CancellationToken.None);

        // SNS
        // SNS: Data bridge events topic
        await SnsClient.GetTopicAttributesAsync(DataBridgeEventsTopicArn, CancellationToken.None);

        // SNS: Import completed topic
        await SnsClient.GetTopicAttributesAsync(KrdsImportCompleteTopicArn, CancellationToken.None);
    }
}