using KeeperData.Core.Orchestration;

namespace KeeperData.Application.Orchestration.ChangeScanning;

public abstract class ScanContext : IScanContext
{
    public Guid ScanCorrelationId { get; init; } = Guid.NewGuid();
}