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
        : this(useFakeAuth: false)
    {
    }

    protected AppTestFixture(bool useFakeAuth = false)
    {
        AppWebApplicationFactory = new AppWebApplicationFactory(useFakeAuth: useFakeAuth);
        HttpClient = AppWebApplicationFactory.CreateClient();

        if (useFakeAuth)
            HttpClient.AddJwt();
        else
            HttpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        DataBridgeApiClientHttpMessageHandlerMock = AppWebApplicationFactory.DataBridgeApiClientHttpMessageHandlerMock;
    }
}