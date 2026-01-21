using KeeperData.Tests.Common.Utilities;
using Moq;

namespace KeeperData.Api.Tests.Component;

public class AppTestFixture
{
    public readonly HttpClient HttpClient;
    public readonly AppWebApplicationFactory AppWebApplicationFactory;
    public readonly Mock<HttpMessageHandler> DataBridgeApiClientHttpMessageHandlerMock;

    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    public AppTestFixture()
    {
        AppWebApplicationFactory = new AppWebApplicationFactory();
        HttpClient = AppWebApplicationFactory.CreateClient();
        HttpClient.AddBasicApiKey(BasicApiKey, BasicSecret);
        DataBridgeApiClientHttpMessageHandlerMock = AppWebApplicationFactory.DataBridgeApiClientHttpMessageHandlerMock;
    }
}