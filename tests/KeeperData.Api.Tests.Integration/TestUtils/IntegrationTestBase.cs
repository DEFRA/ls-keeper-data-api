using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;

namespace KeeperData.Api.Tests.Integration.TestUtils;

[Trait("Dependence", "localstack")]
public class IntegrationTestBase
{
    public readonly TestWebApplicationFactory? WebAppFactory;

    private readonly IAmazonSQS _sqsClient;

    public IntegrationTestBase()
    {
        WebAppFactory = new TestWebApplicationFactory();

        var awsOptions = WebAppFactory.AwsOptions ?? throw new NullReferenceException("You must provide AWS Configuration options");
        _sqsClient = awsOptions.CreateServiceClient<IAmazonSQS>();
    }

    protected async Task PublishMessageAsync(string message)
    {
        // List queues, won't work on CDP because or permissions but will on localstack
        var queueListResult = await _sqsClient.ListQueuesAsync(new ListQueuesRequest());
        if (queueListResult == null || queueListResult.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception("Queue List Request Failed");
        }
        if (queueListResult.QueueUrls.Count <= 0)
        {
            throw new Exception("Queue List Request Succeeded but no queues found");
        }
        var queueUrl = queueListResult.QueueUrls[0];

        // Purge messages from previous test runs
        await _sqsClient.PurgeQueueAsync(queueUrl);

        // Send message
        var request = new SendMessageRequest()
        {
            MessageBody = message,
            QueueUrl = queueUrl,
        };
        await _sqsClient.SendMessageAsync(request);

        // Wait for message poll
        Thread.Sleep(TimeSpan.FromSeconds(3));
    }
}