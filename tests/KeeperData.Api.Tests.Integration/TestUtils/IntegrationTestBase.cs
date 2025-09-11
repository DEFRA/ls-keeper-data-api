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

    protected async Task PublishMessageAsync(string message, string queueUrl)
    {
        var request = new SendMessageRequest()
        {
            MessageBody = message,
            QueueUrl = queueUrl,
        };

        var res = await SqsClient.SendMessageAsync(request);

        // Wait for message poll
        Thread.Sleep(TimeSpan.FromSeconds(3));
    }
}