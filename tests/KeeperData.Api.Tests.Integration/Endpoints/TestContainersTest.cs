using Amazon.S3.Model;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.LocalStack;
using Amazon.SQS.Model;
using Amazon.SQS;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using DotNet.Testcontainers.Builders;
using KeeperData.Api.Tests.Integration.Helpers;
using Docker.DotNet;

namespace KeeperData.Api.Tests.Integration.Endpoints
{
    public class TestContainersTest
    {
        [Fact]
        public async Task LocalStack_CanCreateBucketAndUploadObject()
        {
            var localStackContainer = new LocalStackBuilder()
            .WithImage("localstack/localstack:latest")
            .WithEnvironment("SERVICES", "s3,sqs")
            .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
            .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
            .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
            .WithPortBinding(4566, 4566) // expose edge port
            .Build();

            await localStackContainer.StartAsync();

            try
            {
                // --- Arrange: S3 client pointing to LocalStack ---
                var s3Client = new AmazonS3Client("test", "test", new AmazonS3Config
                {
                    ServiceURL = "http://localhost:4566",
                    ForcePathStyle = true
                });

                var bucketName = "my-test-bucket";
                var objectKey = "hello.txt";

                // --- Act: create bucket and upload object ---
                await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    ContentBody = "Hello LocalStack!"
                });

                // --- Assert: verify object exists ---
                var listResponse = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName
                });

                Assert.Contains(listResponse.S3Objects, o => o.Key == objectKey);
            }
            finally
            {
                // --- Cleanup ---
                await localStackContainer.DisposeAsync();
            }
        }

        [Fact]
        public async Task LocalStack_CanSendAndReceiveSqsMessage()
        {
            // --- Arrange: start LocalStack container with SQS ---
            var localStackContainer = new LocalStackBuilder()
                .WithImage("localstack/localstack:latest")
                .WithEnvironment("SERVICES", "sqs")
                .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
                .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
                .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
                .WithPortBinding(4566, 4566)
                .Build();

            await localStackContainer.StartAsync();

            try
            {
                var sqsClient = new AmazonSQSClient("test", "test", new AmazonSQSConfig
                {
                    ServiceURL = "http://localhost:4566",
                    AuthenticationRegion = "eu-west-2",
                    UseHttp = true
                });

                var queueName = "my-test-queue";

                // --- Act: create queue ---
                var createQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = queueName
                });

                var queueUrl = createQueueResponse.QueueUrl;

                // Send a message
                var messageBody = "Hello SQS!";
                await sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = queueUrl,
                    MessageBody = messageBody
                });

                // Receive the message
                var receiveResponse = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 1
                });

                // --- Assert ---
                Assert.Single(receiveResponse.Messages);
                Assert.Equal(messageBody, receiveResponse.Messages[0].Body);
            }
            finally
            {
                await localStackContainer.DisposeAsync();
            }
        }

        [Fact]
        public async Task LocalStack_CanCreateQueueWithDlqAndRedrivePolicy()
        {
            // --- Arrange: start LocalStack container with SQS ---
            var localStackContainer = new LocalStackBuilder()
                .WithImage("localstack/localstack:latest")
                .WithEnvironment("SERVICES", "sqs")
                .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
                .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
                .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
                .WithPortBinding(4566, 4566)
                .Build();

            await localStackContainer.StartAsync();

            try
            {
                var sqsClient = new AmazonSQSClient("test", "test", new AmazonSQSConfig
                {
                    ServiceURL = "http://localhost:4566",
                    AuthenticationRegion = "eu-west-2",
                    UseHttp = true
                });

                // --- 1. Create the Dead-Letter Queue (DLQ) ---
                var dlqName = "test-deadletter-queue";
                var createDlqResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = dlqName
                });
                var dlqUrl = createDlqResponse.QueueUrl;

                // --- 2. Get the ARN of the DLQ ---
                var dlqAttributes = await sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = dlqUrl,
                    AttributeNames = new List<string> { "QueueArn" }
                });
                var dlqArn = dlqAttributes.QueueARN;

                // --- 3. Create the main SQS queue ---
                var mainQueueName = "test-main-queue";
                var createMainQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = mainQueueName
                });
                var mainQueueUrl = createMainQueueResponse.QueueUrl;

                // --- 4. Define and apply the Redrive Policy ---
                var redrivePolicy = $"{{\"deadLetterTargetArn\":\"{dlqArn}\",\"maxReceiveCount\":\"3\"}}";
                await sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
                {
                    QueueUrl = mainQueueUrl,
                    Attributes = new Dictionary<string, string>
                {
                    { "RedrivePolicy", redrivePolicy }
                }
                });

                // --- 5. Get the SQS Main Queue ARN ---
                var mainQueueAttributes = await sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = mainQueueUrl,
                    AttributeNames = new List<string> { "QueueArn" }
                });
                var mainQueueArn = mainQueueAttributes.QueueARN;

                // --- Assert ---
                Assert.False(string.IsNullOrEmpty(dlqArn));
                Assert.False(string.IsNullOrEmpty(mainQueueArn));

                Console.WriteLine($"DLQ ARN: {dlqArn}");
                Console.WriteLine($"Main Queue ARN: {mainQueueArn}");
            }
            finally
            {
                // Cleanup
                await localStackContainer.DisposeAsync();
            }
        }

        [Fact]
        public async Task LocalStack_CanCreateQueueWithDlqAndRedrivePolicyAndSendAndReceiveMessage()
        {
            var localStackContainer = new LocalStackBuilder()
                .WithImage("localstack/localstack:latest")
                .WithEnvironment("SERVICES", "sqs")
                .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
                .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
                .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
                .WithPortBinding(4566, 4566)
                .Build();

            await localStackContainer.StartAsync();

            try
            {
                var sqsClient = new AmazonSQSClient("test", "test", new AmazonSQSConfig
                {
                    ServiceURL = "http://localhost:4566",
                    AuthenticationRegion = "eu-west-2",
                    UseHttp = true
                });

                // 1. Create DLQ
                var dlqName = "test-dlq";
                var createDlqResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = dlqName });
                var dlqUrl = createDlqResponse.QueueUrl;

                // 2. Get DLQ ARN
                var dlqAttributes = await sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = dlqUrl,
                    AttributeNames = new List<string> { "QueueArn" }
                });
                var dlqArn = dlqAttributes.QueueARN;

                // 3. Create main queue
                var mainQueueName = "test-main-queue";
                var createMainQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = mainQueueName });
                var mainQueueUrl = createMainQueueResponse.QueueUrl;

                // 4. Set redrive policy
                var redrivePolicy = $"{{\"deadLetterTargetArn\":\"{dlqArn}\",\"maxReceiveCount\":\"3\"}}";
                await sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
                {
                    QueueUrl = mainQueueUrl,
                    Attributes = new Dictionary<string, string>
                    {
                        { "RedrivePolicy", redrivePolicy }
                    }
                });

                // 5. Send a message to the main queue
                var messageBody = "Hello DLQ!";
                await sqsClient.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = mainQueueUrl,
                    MessageBody = messageBody
                });

                // 6. Receive the message from the main queue
                var receiveResponse = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = mainQueueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 1
                });

                // Assert: message received
                Assert.Single(receiveResponse.Messages);
                Assert.Equal(messageBody, receiveResponse.Messages[0].Body);

                // Optionally: test DLQ by exceeding maxReceiveCount (not shown here)
            }
            finally
            {
                await localStackContainer.DisposeAsync();
            }
        }


        [Fact]
        public async Task MongoDb_CanInsertAndRetrieveDocument()
        {
            // --- Arrange: start MongoDB container without username/password ---
            var mongoContainer = new MongoDbBuilder()
                .WithImage("mongo:latest")
                .WithPortBinding(27017, 27017) // dynamic host port
                .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "testuser") // disables auth
                .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "testpass")
                .Build();

            await mongoContainer.StartAsync();

            try
            {
                // --- Arrange: MongoClient ---
                var mappedPort = mongoContainer.GetMappedPublicPort(27017);
                var connectionString = $"mongodb://testuser:testpass@localhost:{mappedPort}/testdb?authSource=admin";

                var client = new MongoClient(connectionString);

                var database = client.GetDatabase("testdb");
                var collection = database.GetCollection<BsonDocument>("testcollection");

                // --- Act: insert a document ---
                var doc = new BsonDocument
            {
                { "name", "Alice" },
                { "age", 30 }
            };
                await collection.InsertOneAsync(doc);

                // Retrieve the document
                var retrievedDoc = await collection.Find(new BsonDocument { { "name", "Alice" } }).FirstOrDefaultAsync();

                // --- Assert ---
                Assert.NotNull(retrievedDoc);
                Assert.Equal("Alice", retrievedDoc["name"].AsString);
                Assert.Equal(30, retrievedDoc["age"].AsInt32);
            }
            finally
            {
                // --- Cleanup ---
                await mongoContainer.DisposeAsync();
            }
        }

        [Fact]
        public async Task FullIntegrationTest_WithMongo_LocalStack_AndApi()
        {
            var networkName = "integration-tests";

            DockerNetworkHelper.EnsureNetworkExists(networkName);

            var network = new NetworkBuilder()
                .WithName(networkName)
                .Build();

            // --- MongoDB container ---
            var mongoContainer = new MongoDbBuilder()
              .WithImage("mongo:latest")
              .WithName("mongo")
              .WithPortBinding(27017, true) // dynamic host port
              .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "testuser")
              .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "testpass")
              .WithEnvironment("MONGO_INITDB_DATABASE", "ls-keeper-data-api")
              .WithNetwork(networkName)
              .WithNetworkAliases("mongo")
              .Build();

            // --- LocalStack container ---
            var localStackContainer = new LocalStackBuilder()
                .WithImage("localstack/localstack:latest")
                .WithName("localstack")
                .WithEnvironment("SERVICES", "s3,sqs,sns")
                .WithEnvironment("DEBUG", "1")
                .WithEnvironment("AWS_DEFAULT_REGION", "eu-west-2")
                .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
                .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
                .WithEnvironment("EDGE_PORT", "4566")
                .WithPortBinding(4566, 4566)
                .WithNetwork(networkName)
                .WithNetworkAliases("localstack")
                .Build();

            // --- Start containers ---
            await network.CreateAsync();
            await mongoContainer.StartAsync();
            await localStackContainer.StartAsync();

            // --- API container ---
            var apiContainer = new ContainerBuilder()
             .WithImage("keeperdata_api:latest")
             .WithName("keeperdata_api")
             .WithPortBinding(5555, 5555)
             .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
             .WithEnvironment("ASPNETCORE_HTTP_PORTS", "5555")
             .WithEnvironment("Mongo__DatabaseUri", "mongodb://testuser:testpass@mongo:27017/ls-keeper-data-api?authSource=admin")
             .WithEnvironment("Mongo__DatabaseName", "ls-keeper-data-api")
             .WithEnvironment("StorageConfiguration__ComparisonReportsStorage__BucketName", "test-comparison-reports-bucket")
             .WithEnvironment("QueueConsumerOptions__IntakeEventQueueOptions__QueueUrl", "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue")
             .WithEnvironment("QueueConsumerOptions__IntakeEventQueueOptions__DeadLetterQueueUrl", "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue-deadletter\r\n      - ApiClients__DataBridgeApi__BaseUrl=http://keeperdata_bridge:5560/")
             .WithEnvironment("ApiClients__DataBridgeApi__BaseUrl", "http://localhost:5560/")
             .WithEnvironment("ApiClients__DataBridgeApi__UseFakeClient", "true")
             .WithEnvironment("ServiceBusSenderConfiguration__IntakeEventQueue__QueueUrl", "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue")
             .WithEnvironment("LOCALSTACK_ENDPOINT", "http://localstack:4566")
             .WithEnvironment("AWS__Region", "eu-west-2")
             .WithEnvironment("AWS_REGION", "eu-west-2")
             .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
             .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
             .WithEnvironment("AWS__ServiceURL", "http://localstack:4566")
             .WithNetwork(networkName)
             .WithNetworkAliases("keeperdata_api")
             .WithWaitStrategy(Wait.ForUnixContainer()
                 .UntilHttpRequestIsSucceeded(req => req.ForPort(5555).ForPath("/health")))
             .Build();

            try
            {
                // --- Arrange: S3 client pointing to LocalStack ---
                var s3Client = new AmazonS3Client("test", "test", new AmazonS3Config
                {
                    ServiceURL = "http://localhost:4566",
                    ForcePathStyle = true
                });

                var bucketName = "test-comparison-reports-bucket";
                var objectKey = "hello.txt";

                // --- Act: create bucket and upload object ---
                await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    ContentBody = "Hello LocalStack!"
                });

                // --- Assert: verify object exists ---
                var listResponse = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName
                });

                Assert.Contains(listResponse.S3Objects, o => o.Key == objectKey);

                var credentials = new Amazon.Runtime.BasicAWSCredentials("test", "test");
                var sqsClient = new AmazonSQSClient(credentials, new AmazonSQSConfig
                {
                    ServiceURL = "http://localhost:4566",
                    AuthenticationRegion = "eu-west-2",
                    UseHttp = true
                });

                // 1. Create DLQ
                var dlqName = "ls_keeper_data_intake_queue-deadletter";
                await sqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = dlqName });

                var createDlqResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = dlqName });
                var dlqUrl = createDlqResponse.QueueUrl;

                // 2. Get DLQ ARN
                var dlqAttributes = await sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = dlqUrl,
                    AttributeNames = new List<string> { "QueueArn" }
                });
                var dlqArn = dlqAttributes.QueueARN;

                // 3. Create main queue
                var mainQueueName = "ls_keeper_data_intake_queue";
                var createMainQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = mainQueueName });
                var mainQueueUrl = createMainQueueResponse.QueueUrl;

                // 4. Set redrive policy
                var redrivePolicy = $"{{\"deadLetterTargetArn\":\"{dlqArn}\",\"maxReceiveCount\":\"3\"}}";
                await sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
                {
                    QueueUrl = mainQueueUrl,
                    Attributes = new Dictionary<string, string>
                    {
                        { "RedrivePolicy", redrivePolicy }
                    }
                });

                // 5. Send a message to the main queue
                //var messageBody = "Hello DLQ!";
                //await sqsClient.SendMessageAsync(new SendMessageRequest
                //{
                //    QueueUrl = mainQueueUrl,
                //    MessageBody = messageBody
                //});

                //// 6. Receive the message from the main queue
                //var receiveResponse = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                //{
                //    QueueUrl = mainQueueUrl,
                //    MaxNumberOfMessages = 1,
                //    WaitTimeSeconds = 1
                //});

                //// Assert: message received
                //Assert.Single(receiveResponse.Messages);
                //Assert.Equal(messageBody, receiveResponse.Messages[0].Body);


                // --- Wait until MongoDB is ready ---
                var mappedPortMongo = mongoContainer.GetMappedPublicPort(27017);
                var connectionString = $"mongodb://localhost:{mappedPortMongo}/testdb";

                var mongoClient = new MongoClient(connectionString);

                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        await mongoClient.GetDatabase("admin")
                            .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
                        break;
                    }
                    catch
                    {
                        await Task.Delay(1000);
                    }
                }

                // --- Test API health ---
                await apiContainer.StartAsync();

                using var httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{apiContainer.GetMappedPublicPort(5555)}") };
                var response = await httpClient.GetAsync("health");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("healthy", content, StringComparison.OrdinalIgnoreCase);

                // --- Example MongoDB insert and retrieve ---
                //var testDb = mongoClient.GetDatabase("testdb");
                //var testColl = testDb.GetCollection<MongoDB.Bson.BsonDocument>("test");
                //var doc = new MongoDB.Bson.BsonDocument { { "Name", "Vihaan" }, { "Age", 5 } };
                //await testColl.InsertOneAsync(doc);

                //var result = await testColl.Find(Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("Name", "Vihaan")).FirstOrDefaultAsync();
                //Assert.NotNull(result);
            }
            finally
            {
                // --- Cleanup ---
                await apiContainer.DisposeAsync();
                await localStackContainer.DisposeAsync();
                await mongoContainer.DisposeAsync();
                await network.DeleteAsync();
            }
        }
    }
}
