namespace KeeperData.Application.Orchestration.ChangeScanning.BaseClasses
{
    public interface IBulkScanContext
    {
        public DateTime CurrentDateTime { get; init; }
        public DateTime? UpdatedSinceDateTime { get; init; }
        public int PageSize { get; init; }
        public EntityScanContext Holdings { get; init; }
    }
}