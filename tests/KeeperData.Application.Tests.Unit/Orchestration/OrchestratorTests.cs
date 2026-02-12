using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Application.Orchestration.Imports;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Updates;
using KeeperData.Application.Orchestration.Updates.Cts.Agents;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers;
using KeeperData.Core.Telemetry;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration;

public class OrchestratorTests
{
    private readonly Mock<IApplicationMetrics> _mockMetrics = new();

    [Fact]
    public async Task CtsBulkScanOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IScanStep<CtsBulkScanContext>>();
        var orchestrator = new CtsBulkScanOrchestrator([step.Object], _mockMetrics.Object);
        var context = new CtsBulkScanContext();

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CtsDailyScanOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IScanStep<CtsDailyScanContext>>();
        var orchestrator = new CtsDailyScanOrchestrator([step.Object], _mockMetrics.Object);
        var context = new CtsDailyScanContext();

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SamBulkScanOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IScanStep<SamBulkScanContext>>();
        var orchestrator = new SamBulkScanOrchestrator([step.Object], _mockMetrics.Object);
        var context = new SamBulkScanContext();

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SamDailyScanOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IScanStep<SamDailyScanContext>>();
        var orchestrator = new SamDailyScanOrchestrator([step.Object], _mockMetrics.Object);
        var context = new SamDailyScanContext();

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CtsHoldingImportOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IImportStep<CtsHoldingImportContext>>();
        var orchestrator = new CtsHoldingImportOrchestrator([step.Object]);
        var context = new CtsHoldingImportContext { Cph = "123" };

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SamHoldingImportOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IImportStep<SamHoldingImportContext>>();
        var orchestrator = new SamHoldingImportOrchestrator([step.Object]);
        var context = new SamHoldingImportContext { Cph = "123" };

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CtsUpdateHoldingOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IUpdateStep<CtsUpdateHoldingContext>>();
        var orchestrator = new CtsUpdateHoldingOrchestrator([step.Object]);
        var context = new CtsUpdateHoldingContext { Cph = "123" };

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CtsUpdateAgentOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IUpdateStep<CtsUpdateAgentContext>>();
        var orchestrator = new CtsUpdateAgentOrchestrator([step.Object]);
        var context = new CtsUpdateAgentContext { PartyId = "123" };

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CtsUpdateKeeperOrchestrator_ExecuteAsync_CallsSteps()
    {
        var step = new Mock<IUpdateStep<CtsUpdateKeeperContext>>();
        var orchestrator = new CtsUpdateKeeperOrchestrator([step.Object]);
        var context = new CtsUpdateKeeperContext { PartyId = "123" };

        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        step.Verify(x => x.ExecuteAsync(context, CancellationToken.None), Times.Once);
    }
}