using Moq;

namespace KeeperData.Api.Tests.Component;

public class AppTestFixture : IDisposable
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppWebApplicationFactory?.Dispose();
        }
    }
}