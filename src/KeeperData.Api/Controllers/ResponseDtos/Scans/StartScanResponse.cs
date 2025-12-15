namespace KeeperData.Api.Controllers.ResponseDtos.Scans
{
    public class StartScanResponse
    {
        public Guid ScanCorrelationId { get; set; }
        public string? Message { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}