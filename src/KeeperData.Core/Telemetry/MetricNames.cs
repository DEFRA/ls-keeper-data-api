namespace KeeperData.Core.Telemetry;

public static class MetricNames
{
    public const string MeterName = "KeeperData";
    public const string Batch = "keeperdata.batch";
    public const string Repository = "keeperdata.repository";
    public const string DataBridge = "keeperdata.databridge";
    public const string Queue = "keeperdata.queue";
    public const string Orchestrator = "keeperdata.orchestrator";

    public static class CommonTags
    {
        public const string Service = "keeperdata.service";
        public const string HealthCheck = "keeperdata.healthcheck";
        public const string Status = "keeperdata.status";
        public const string Operation = "operation";
        public const string Collection = "collection";
        public const string ErrorType = "error_type";
        public const string BatchSize = "batch_size";
        public const string UpdateType = "update_type";
    }
    
    public static class Operations
    {
        // Batch operations
        public const string BatchStarted = "started";
        public const string BatchDuration = "duration";
        public const string BatchRecordsPerSecond = "records_per_second";
        public const string BatchProcessed = "processed";
        public const string BatchFailed = "failed";
        
        // Repository operations
        public const string BulkCreateStarted = "bulk_create_started";
        public const string BulkCreateDuration = "bulk_create_duration";
        public const string BulkCreateSuccess = "bulk_create_success";
        public const string BulkCreateFailed = "bulk_create_failed";
        public const string BulkUpdateStarted = "bulk_update_started";
        public const string BulkUpdateDuration = "bulk_update_duration";
        public const string BulkUpdateSuccess = "bulk_update_success";
        public const string BulkUpdateFailed = "bulk_update_failed";
        
        // Queue operations
        public const string QueueBatchProcessed = "queue_batch_processed";
        public const string QueueBatchFailed = "queue_batch_failed";
        public const string QueueMessageProcessed = "message_processed";
        public const string QueueMessageFailed = "message_failed";
        
        // DataBridge operations
        public const string PagedRequestStarted = "paged_request_started";
        public const string PagedRequestDuration = "paged_request_duration";
        public const string PagedRequestRecords = "paged_request_records";
        public const string PagedRequestFailed = "paged_request_failed";
        
        // Orchestrator operations
        public const string OrchestrationStarted = "orchestration_started";
        public const string OrchestrationDuration = "orchestration_duration";
        public const string OrchestrationSuccess = "orchestration_success";
        public const string OrchestrationFailed = "orchestration_failed";
    }
}