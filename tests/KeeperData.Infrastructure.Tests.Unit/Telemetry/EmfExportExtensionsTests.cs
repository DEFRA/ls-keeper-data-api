using FluentAssertions;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Diagnostics.Metrics;

namespace KeeperData.Infrastructure.Tests.Unit.Telemetry;

public class EmfExportExtensionsTests
{
    [Fact]
    public void UseEmfExporter_WhenCalled_ShouldReturnApplicationBuilder()
    {
        // Arrange
        var mockBuilder = new Mock<IApplicationBuilder>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockLogger = new Mock<ILogger>();
        var mockAwsConfigOptions = new Mock<IOptions<AwsConfig>>();

        var awsConfig = new AwsConfig
        {
            EMF = new EmfConfig { Namespace = "TestNamespace" }
        };

        mockAwsConfigOptions.Setup(x => x.Value).Returns(awsConfig);
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<AwsConfig>))).Returns(mockAwsConfigOptions.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(ILoggerFactory))).Returns(mockLoggerFactory.Object);
        mockBuilder.Setup(x => x.ApplicationServices).Returns(mockServiceProvider.Object);

        // Act
        var result = mockBuilder.Object.UseEmfExporter();

        // Assert
        result.Should().BeSameAs(mockBuilder.Object);
        mockAwsConfigOptions.Verify(x => x.Value, Times.Once);
        mockLoggerFactory.Verify(x => x.CreateLogger(nameof(EmfExporter)), Times.Once);
    }
}