using FluentAssertions;
using KeeperData.Api.Worker.Tasks;
using KeeperData.Tests.Common.Utilities;
using Moq;
using System.Net;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class ImportEndpointTests
{
    private readonly Mock<ICtsBulkScanTask> _ctsBulkScanTaskMock = new();
    private readonly Mock<ISamBulkScanTask> _samBulkScanTaskMock = new();
    private readonly Mock<ICtsDailyScanTask> _ctsDailyScanTaskMock = new();
    private readonly Mock<ISamDailyScanTask> _samDailyScanTaskMock = new();

    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    public ImportEndpointTests()
    {
        _ctsBulkScanTaskMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        _samBulkScanTaskMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        _ctsDailyScanTaskMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        _samDailyScanTaskMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task GivenCtsBulkScanEndpointsDisabled_WhenRequestMadeToEndpoint_ShouldReturnNotFound()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartCtsBulkScanEndpoint,
            _ctsBulkScanTaskMock.Object);
    }

    [Fact]
    public async Task GivenSamBulkScanEndpointsDisabled_WhenRequestMadeToEndpoint_ShouldReturnNotFound()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartSamBulkScanEndpoint,
            _samBulkScanTaskMock.Object);
    }

    [Fact]
    public async Task GivenCtsBulkScanEndpointsEnabled_WhenRequestMadeToEndpoint_ShouldSucceed()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartCtsBulkScanEndpoint,
            _ctsBulkScanTaskMock.Object,
            bulkScanEnabled: true,
            expectedStatusCode: HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GivenSamBulkScanEndpointsEnabled_WhenRequestMadeToEndpoint_ShouldSucceed()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartSamBulkScanEndpoint,
            _samBulkScanTaskMock.Object,
            bulkScanEnabled: true,
            expectedStatusCode: HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GivenCtsDailyScanEndpointsDisabled_WhenRequestMadeToEndpoint_ShouldReturnNotFound()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartCtsDailyScanEndpoint,
            _ctsDailyScanTaskMock.Object);
    }

    [Fact]
    public async Task GivenSamDailyScanEndpointsDisabled_WhenRequestMadeToEndpoint_ShouldReturnNotFound()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartSamDailyScanEndpoint,
            _samDailyScanTaskMock.Object);
    }

    [Fact]
    public async Task GivenCtsDailyScanEndpointsEnabled_WhenRequestMadeToEndpoint_ShouldSucceed()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartCtsDailyScanEndpoint,
            _ctsDailyScanTaskMock.Object,
            dailyScanEnabled: true,
            expectedStatusCode: HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GivenSamDailyScanEndpointsEnabled_WhenRequestMadeToEndpoint_ShouldSucceed()
    {
        await ExecuteScanEndpointTest(TestConstants.ImportStartSamDailyScanEndpoint,
            _samDailyScanTaskMock.Object,
            dailyScanEnabled: true,
            expectedStatusCode: HttpStatusCode.Accepted);
    }

    private static async Task ExecuteScanEndpointTest<TService>(
        string endpoint,
        TService service,
        bool bulkScanEnabled = false,
        bool dailyScanEnabled = false,
        HttpStatusCode expectedStatusCode = HttpStatusCode.NotFound) where TService : class
    {
        var configurationOverrides = new Dictionary<string, string?>
        {
            ["BulkScanEndpointsEnabled"] = bulkScanEnabled.ToString().ToLowerInvariant(),
            ["DailyScanEndpointsEnabled"] = dailyScanEnabled.ToString().ToLowerInvariant()
        };

        var factory = new AppWebApplicationFactory(configurationOverrides);
        factory.OverrideServiceAsSingleton(service);

        var httpClient = factory.CreateClient();
        httpClient.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await httpClient.PostAsync(endpoint, null);

        response.StatusCode.Should().Be(expectedStatusCode);
    }
}