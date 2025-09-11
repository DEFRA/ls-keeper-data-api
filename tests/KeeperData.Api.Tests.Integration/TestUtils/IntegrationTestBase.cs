using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Api.Tests.Integration.TestUtils;

[Trait("Dependence", "localstack")]
public class IntegrationTestBase
{
    protected static TestWebApplicationFactory? WebAppFactory;

    private static IAmazonSQS SqsClient { get; }

    static IntegrationTestBase()
    {
        WebAppFactory ??= new TestWebApplicationFactory();

        var awsOptions = WebAppFactory.AwsOptions;

        if (awsOptions == null)
        {
            throw new NullReferenceException("You must provide AWS Configuration options");
        }

        SqsClient = awsOptions.CreateServiceClient<IAmazonSQS>();
    }

    protected async Task PublishMessageAsync(string message, string queueName)
    {
        // List queues, won't work on CDP because or permissions but will on localstack
        var queueListResult = await SqsClient.ListQueuesAsync(queueName);
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
        await SqsClient.PurgeQueueAsync(queueUrl);

        // Send message
        var request = new SendMessageRequest()
        {
            MessageBody = message,
            QueueUrl = queueUrl,
        };
        await SqsClient.SendMessageAsync(request);

        // Wait for message poll
        Thread.Sleep(TimeSpan.FromSeconds(3));
    }
}