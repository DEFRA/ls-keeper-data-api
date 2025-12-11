namespace KeeperData.Core.Orchestration;

public interface IScanContext
{
    Guid ScanCorrelationId { get; }
}