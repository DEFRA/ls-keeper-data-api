using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk.Steps;
using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam.Bulk.Steps;

public class SamBulkScanCompletionStepTests
{
    private readonly Mock<IBatchCompletionNotificationService> _mockBatchCompletionService = new();
    private readonly Mock<ILogger<SamBulkScanCompletionStep>> _mockLogger = new();
    private readonly SamBulkScanCompletionStep _sut;

    public SamBulkScanCompletionStepTests()
    {
        _sut = new SamBulkScanCompletionStep(_mockBatchCompletionService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_CallsBatchCompletionService()
    {
        // Arrange
        var context = new SamBulkScanContext
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
        var context = new SamBulkScanContext();
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
        var context = new SamBulkScanContext();
        var expectedException = new InvalidOperationException("Test exception");

        _mockBatchCompletionService.Setup(x => x.NotifyBatchCompletionAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var act = () => _sut.ExecuteAsync(context, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Test exception");
    }

    [Fact]
    public void StepOrder_ShouldBe999()
    {
        // Arrange & Act
        var stepOrderAttribute = typeof(SamBulkScanCompletionStep)
            .GetCustomAttributes(typeof(StepOrderAttribute), false)
            .FirstOrDefault() as StepOrderAttribute;

        // Assert
        stepOrderAttribute.Should().NotBeNull();
        stepOrderAttribute!.Order.Should().Be(999);
    }
}