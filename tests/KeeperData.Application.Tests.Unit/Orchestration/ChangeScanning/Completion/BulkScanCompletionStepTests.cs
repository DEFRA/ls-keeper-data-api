using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Completion;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Completion;

public class BulkScanCompletionStepTests
{
    private readonly Mock<IBatchCompletionNotificationService> _mockBatchCompletionService = new();
    private readonly Mock<ILogger<BulkScanCompletionStep>> _mockLogger = new();
    private readonly BulkScanCompletionStep _sut;

    public BulkScanCompletionStepTests()
    {
        _sut = new BulkScanCompletionStep(_mockBatchCompletionService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_CallsBatchCompletionService()
    {
        // Arrange
        var context = new SamBulkScanContext()
        {
            ScanCorrelationId = Guid.NewGuid(),
            CurrentDateTime = DateTime.UtcNow,
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
    public void StepOrder_ShouldBe2()
    {
        // Arrange & Act
        var stepOrderAttribute = typeof(BulkScanCompletionStep)
            .GetCustomAttributes(typeof(StepOrderAttribute), false)
            .FirstOrDefault() as StepOrderAttribute;

        // Assert
        stepOrderAttribute.Should().NotBeNull();
        stepOrderAttribute!.Order.Should().Be(2);
    }
}