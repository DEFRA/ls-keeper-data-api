using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;
using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam;

public class SamPartyDailyScanCompletionStepTests
{
    private readonly Mock<IBatchCompletionNotificationService> _mockBatchCompletionService = new();
    private readonly Mock<ILogger<SamPartyDailyScanCompletionStep>> _mockLogger = new();
    private readonly SamPartyDailyScanCompletionStep _sut;

    public SamPartyDailyScanCompletionStepTests()
    {
        _sut = new SamPartyDailyScanCompletionStep(_mockBatchCompletionService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_CallsBatchCompletionService()
    {
        // Arrange
        var context = new SamDailyScanContext
        {
            ScanCorrelationId = Guid.NewGuid(),
            CurrentDateTime = DateTime.UtcNow,
            Holders = new EntityScanContext { TotalCount = 100, CurrentCount = 10 },
            Holdings = new EntityScanContext { TotalCount = 200, CurrentCount = 20 }
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _mockBatchCompletionService.Verify(
            x => x.NotifyBatchCompletionAsync(context, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_PassesCancellationToken()
    {
        // Arrange
        var context = new SamDailyScanContext();
        var cancellationToken = new CancellationToken(canceled: true);

        // Act
        await _sut.ExecuteAsync(context, cancellationToken);

        // Assert
        _mockBatchCompletionService.Verify(
            x => x.NotifyBatchCompletionAsync(context, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBatchCompletionServiceThrows_ExceptionPropagates()
    {
        // Arrange
        var context = new SamDailyScanContext();
        var expectedException = new InvalidOperationException("Test exception");

        _mockBatchCompletionService.Setup(x => x.NotifyBatchCompletionAsync(It.IsAny<SamDailyScanContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var act = () => _sut.ExecuteAsync(context, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
    }

    [Fact]
    public void StepOrder_ShouldBe5()
    {
        // Arrange & Act
        var stepOrderAttribute = typeof(SamPartyDailyScanCompletionStep)
            .GetCustomAttributes(typeof(StepOrderAttribute), false)
            .FirstOrDefault() as StepOrderAttribute;

        // Assert
        stepOrderAttribute.Should().NotBeNull();
        stepOrderAttribute!.Order.Should().Be(5);
    }
}