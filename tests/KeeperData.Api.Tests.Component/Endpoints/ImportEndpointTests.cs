using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Tests.Common.Utilities;
using Moq;
using System.Net;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class ImportEndpointTests
{
    private readonly Mock<ICtsScanTask> _ctsScanTaskMock = new();
    private readonly Mock<ISamScanTask> _samScanTaskMock = new();

    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    public ImportEndpointTests()
    {
        _ctsScanTaskMock.Setup(x => x.StartAsync(It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        _samScanTaskMock.Setup(x => x.StartAsync(It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task GivenCtsScanEndpointsDisabled_WhenRequestMadeToEndpoint_ShouldReturnNotFound()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartCtsScanEndpoint,
            _ctsScanTaskMock.Object);
    }

    [Fact]
    public async Task GivenSamScanEndpointsDisabled_WhenRequestMadeToEndpoint_ShouldReturnNotFound()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartSamScanEndpoint,
            _samScanTaskMock.Object);
    }

    [Fact]
    public async Task GivenCtsScanEndpointsEnabled_WhenRequestMadeToEndpoint_ShouldSucceed()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartCtsScanEndpoint,
            _ctsScanTaskMock.Object,
            scanEndpointsEnabled: true,
            expectedStatusCode: HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GivenSamScanEndpointsEnabled_WhenRequestMadeToEndpoint_ShouldSucceed()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartSamScanEndpoint,
            _samScanTaskMock.Object,
            scanEndpointsEnabled: true,
            expectedStatusCode: HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GivenCtsScanEndpointEnabled_WhenSinceHoursProvided_ShouldForwardSinceHoursToTask()
    {
        const int sinceHours = 24;

        await ExecuteScanEndpointWithParamsTest(
            TestConstants.ImportStartCtsScanEndpoint,
            _ctsScanTaskMock,
            sinceHours: sinceHours);

        _ctsScanTaskMock.Verify(x => x.StartAsync(false, sinceHours, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenSamScanEndpointEnabled_WhenSinceHoursNotProvided_ShouldForwardNullSinceHoursToTask()
    {
        await ExecuteScanEndpointWithParamsTest(
            TestConstants.ImportStartSamScanEndpoint,
            _samScanTaskMock,
            sinceHours: null);

        _samScanTaskMock.Verify(x => x.StartAsync(false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenCtsScanEndpointEnabled_WhenForceBulkProvided_ShouldForwardForceBulkToTask()
    {
        await ExecuteScanEndpointWithParamsTest(
            TestConstants.ImportStartCtsScanEndpoint,
            _ctsScanTaskMock,
            forceBulk: true);

        _ctsScanTaskMock.Verify(x => x.StartAsync(true, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task ExecuteScanEndpointWithParamsTest<TService>(
        string endpoint,
        Mock<TService> serviceMock,
        int? sinceHours = null,
        bool? forceBulk = null) where TService : class
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["ScanEndpointsEnabled"] = "true"
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(serviceMock.Object);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var queryParams = new List<string>();
        if (sinceHours.HasValue)
            queryParams.Add($"sinceHours={sinceHours.Value}");
        if (forceBulk.HasValue)
            queryParams.Add($"forceBulk={forceBulk.Value.ToString().ToLowerInvariant()}");

        var url = queryParams.Count > 0
            ? $"{endpoint}?{string.Join("&", queryParams)}"
            : endpoint;

        var response = await httpClient.PostAsync(url, null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private static async Task ExecuteScanEndpointTest<TService>(
        string endpoint,
        TService service,
        bool scanEndpointsEnabled = false,
        HttpStatusCode expectedStatusCode = HttpStatusCode.NotFound) where TService : class
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["ScanEndpointsEnabled"] = scanEndpointsEnabled.ToString().ToLowerInvariant()
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(service);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync(endpoint, null);

        response.StatusCode.Should().Be(expectedStatusCode);
    }
}
