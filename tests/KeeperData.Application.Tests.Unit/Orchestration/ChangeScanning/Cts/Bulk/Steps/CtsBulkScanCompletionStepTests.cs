using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk.Steps;
using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Cts.Bulk.Steps;

public class CtsBulkScanCompletionStepTests
{
    private readonly Mock<IBatchCompletionNotificationService> _mockBatchCompletionService = new();
    private readonly Mock<ILogger<CtsBulkScanCompletionStep>> _mockLogger = new();
    private readonly CtsBulkScanCompletionStep _sut;

    public CtsBulkScanCompletionStepTests()
    {
        _sut = new CtsBulkScanCompletionStep(_mockBatchCompletionService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_CallsBatchCompletionService()
    {
        // Arrange
        var context = new CtsBulkScanContext
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
    public void StepOrder_ShouldBe999()
    {
        // Arrange & Act
        var stepOrderAttribute = typeof(CtsBulkScanCompletionStep)
            .GetCustomAttributes(typeof(StepOrderAttribute), false)
            .FirstOrDefault() as StepOrderAttribute;

        // Assert
        stepOrderAttribute.Should().NotBeNull();
        stepOrderAttribute!.Order.Should().Be(999);
    }
}