using Moq;

namespace KeeperData.Api.Tests.Component;

public class AppTestFixture
{
    public readonly HttpClient HttpClient;
    public readonly AppWebApplicationFactory AppWebApplicationFactory;
    public readonly Mock<HttpMessageHandler> DataBridgeApiClientHttpMessageHandlerMock;

    public AppTestFixture()
    {
        AppWebApplicationFactory = new AppWebApplicationFactory();
        HttpClient = AppWebApplicationFactory.CreateClient();
        DataBridgeApiClientHttpMessageHandlerMock = AppWebApplicationFactory.DataBridgeApiClientHttpMessageHandlerMock;
    }
}