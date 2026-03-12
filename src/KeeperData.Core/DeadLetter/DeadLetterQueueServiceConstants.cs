using KeeperData.Core.ApiClients.DataBridgeApi.Converters;

namespace KeeperData.Core.DeadLetter;

public static class DeadLetterQueueServiceConstants
{
    public const string StringDataType = "String";
    public const string NumberDataType = "Number";
    public const string AllAttributes = "All";

    public static class Tags
    {
        public const string DeadLetterQueue = "Dead Letter Queue";
    }

    public static class SqsAttributes
    {
        public const string ApproximateNumberOfMessages = "ApproximateNumberOfMessages";
        public const string ApproximateNumberOfMessagesNotVisible = "ApproximateNumberOfMessagesNotVisible";
        public const string ApproximateNumberOfMessagesDelayed = "ApproximateNumberOfMessagesDelayed";
        public const string ApproximateReceiveCount = "ApproximateReceiveCount";
    }

    public static class MessageAttributes
    {
        public const string CorrelationId = "CorrelationId";
        public const string Subject = "Subject";
        public const string DlqOriginalMessageId = "DLQ_OriginalMessageId";
        public const string DlqFailureReason = "DLQ_FailureReason";
        public const string DlqFailureMessage = "DLQ_FailureMessage";
        public const string DlqFailureTimestamp = "DLQ_FailureTimestamp";
        public const string DlqReceiveCount = "DLQ_ReceiveCount";
        public const string DlqPrefix = "DLQ_";
    }

    public static class Timeouts
    {
        public const int ExtendedVisibilitySeconds = 300;
        public const int ReceiveMessageVisibilitySeconds = 60;
        public const int PeekVisibilitySeconds = 0;
    }

    public static class Limits
    {
        public const int MaxSqsMessageAttributeLength = 256;
        public const int MaxSqsReceiveMessages = 10;
    }

    public static class LogMessages
    {
        public const string NoDlqConfigured = "No DLQ configured for message {MessageId}";
        public const string SentToDlq = "Message {OriginalMessageId} sent to DLQ with new ID {DlqMessageId}";
        public const string MovedToDlq = "Message {MessageId} successfully moved to DLQ";
        public const string SendSucceededDeleteFailed = "CRITICAL: Sent to DLQ but DELETE FAILED - DUPLICATE WILL OCCUR";
        public const string FailedToSend = "Failed to send to DLQ - message will retry";
        public const string DeleteFailed = "Send succeeded but delete failed for MessageId: {MessageId}";
        public const string PurgingQueue = "Purging dead letter queue {QueueUrl}. Approximate messages to be purged: {Count}. This is a destructive operation.";
        public const string RedroveFromDlq = "Redrove message {MessageId} with CorrelationId {CorrelationId} from DLQ to main queue.";
        public const string FailedToSendToMainQueue = "Failed to send message {MessageId} to main queue during redrive.";
        public const string FailedToDeleteFromDlq = "Failed to delete message {MessageId} from DLQ after successful send. CorrelationId: {CorrelationId}";
        public const string DuplicateDetected = "CRITICAL: DUPLICATE message detected. Message {MessageId} exists in both queues. CorrelationId: {CorrelationId}";
        public const string DeadLetterQueueUrlNotConfiguredError = "DeadLetterQueueUrl is not configured.";
        public const string UnableToReachDeadLetterQueueError = "Unable to reach dead letter queue";
        public const string PurgeInProgressError = "A purge operation is already in progress. Try again in 60 seconds.";
        public const string RequestCancelledError = "Request was cancelled.";
    }
}