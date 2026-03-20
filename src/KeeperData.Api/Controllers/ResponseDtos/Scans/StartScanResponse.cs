namespace KeeperData.Api.Controllers.ResponseDtos.Scans
{
    /// <summary>
    /// Response returned when a scan operation is started.
    /// </summary>
    public class StartScanResponse
    {
        /// <summary>
        /// The unique correlation identifier for tracking this scan operation.
        /// </summary>
        public Guid ScanCorrelationId { get; set; }

        /// <summary>
        /// A human-readable message describing the scan status.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// The timestamp when the scan was started.
        /// </summary>
        public DateTime? StartedAt { get; set; }
    }
}